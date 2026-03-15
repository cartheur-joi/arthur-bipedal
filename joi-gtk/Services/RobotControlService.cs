using Cartheur.Animals.Robot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace joi_gtk.Services;

public sealed class RobotControlService
{
    readonly MotorFunctions _motorControl;
    readonly WalkController _walkController;
    readonly object _initializeGate = new();
    readonly object _busIoGate = new();
    readonly Dictionary<string, int> _motorOverloadThresholds = new(StringComparer.Ordinal);
    readonly string _safetyEventLogPath;
    int _safetyOverloadThreshold = MotorFunctions.PresentLoadAlarm;
    bool _initialized;
    public event Action<SafetyGateTrip> SafetyGateTripped;

    public RobotControlService()
    {
        _motorControl = new MotorFunctions();
        _walkController = new WalkController(_motorControl);
        LoadMotorOverloadThresholdPolicy();
        _safetyEventLogPath = ResolveSafetyEventLogPath();
    }

    public string Initialize()
    {
        lock (_initializeGate)
        {
            if (_initialized)
                return "Robot already initialized.";

            EnsureNativeDynamixelPrerequisites();
            EnsureMaps();
            AutoConfigureLinuxBusMapping();
            _motorControl.SetActivePorts();
            string result = _motorControl.InitializeDynamixelMotors().Trim();
            if (!MotorFunctions.DynamixelMotorsInitialized)
                throw new InvalidOperationException(result);

            int respondingLowerMotors = ScanLowerMotors();
            _initialized = true;
            IReadOnlyList<MotorMonitorReading> snapshot = ReadMotorMonitoringSnapshot(MotorFunctions.PresentLoadAlarm);
            int respondingUpperMotors = snapshot.Count(r => r.Location == "upper" && r.CommunicationOk);
            int communicationErrors = snapshot.Count(r => !r.CommunicationOk);
            int overloads = snapshot.Count(r => r.Overload);
            return
                $"{result} Lower motor scan OK ({respondingLowerMotors} responding). " +
                $"Upper motor scan OK ({respondingUpperMotors} responding). " +
                $"Errors={communicationErrors}, Overloads={overloads}.";
        }
    }

    public bool IsInitialized => _initialized;
    public int SafetyOverloadThreshold
    {
        get => _safetyOverloadThreshold;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Safety overload threshold must be > 0.");
            _safetyOverloadThreshold = value;
        }
    }

    public string TorqueOnLower()
    {
        return ExecuteOnBus("TorqueOnLower", () =>
        {
            VerifyLowerBus("TorqueOnLower");
            _motorControl.SetTorqueOn("lower");
            EnsureLastTxRxSuccessOnLower("TorqueOnLower");
            return "Lower-body torque enabled.";
        });
    }

    public string TorqueOffLower()
    {
        return ExecuteOnBus("TorqueOffLower", () =>
        {
            VerifyLowerBus("TorqueOffLower");
            _motorControl.SetTorqueOff("lower");
            EnsureLastTxRxSuccessOnLower("TorqueOffLower");
            return "Lower-body torque disabled.";
        });
    }

    public string ReadLowerTelemetry()
    {
        return ExecuteOnBus("ReadLowerTelemetry", () =>
        {
            VerifyLowerBus("ReadLowerTelemetry");
            Dictionary<string, int> snapshot = _motorControl.GetPresentPositions(Limbic.LeftLeg);
            foreach (KeyValuePair<string, int> kv in _motorControl.GetPresentPositions(Limbic.RightLeg))
                snapshot[kv.Key] = kv.Value;

            return "Lower pose: " + string.Join(", ", snapshot.Select(kv => $"{kv.Key}={kv.Value}"));
        });
    }

    public Dictionary<string, int> ReadPositions(string[] motors)
    {
        return ExecuteOnBus("ReadPositions", () =>
        {
            if (motors == null || motors.Length == 0)
                throw new InvalidOperationException("ReadPositions requires one or more motors.");

            return _motorControl.GetPresentPositions(motors);
        });
    }

    public void SetTorqueOff(string[] motors)
    {
        ExecuteOnBus("SetTorqueOff", () =>
        {
            if (motors == null || motors.Length == 0)
                throw new InvalidOperationException("SetTorqueOff requires one or more motors.");

            _motorControl.SetTorqueOff(motors);
            return true;
        });
    }

    public void SetTorqueOn(string[] motors)
    {
        ExecuteOnBus("SetTorqueOn", () =>
        {
            if (motors == null || motors.Length == 0)
                throw new InvalidOperationException("SetTorqueOn requires one or more motors.");

            _motorControl.SetTorqueOn(motors);
            return true;
        });
    }

    public void MoveToPositions(Dictionary<string, int> targets, int durationMilliseconds = 700, int interpolationSteps = 6)
    {
        ExecuteMotionWithSafety("MoveToPositions", targets?.Keys, () =>
        {
            if (targets == null || targets.Count == 0)
                throw new InvalidOperationException("MoveToPositions requires one or more target motors.");

            _motorControl.MoveMotorSequenceSmooth(targets, durationMilliseconds, interpolationSteps);
            return true;
        });
    }

    public string ExecuteWalkCycleSupervised(int cycles, int stepDurationMs, int interpolationSteps, int timeoutMs, bool requireSupportFootContact)
    {
        IEnumerable<string> lowerBodyMotors = Limbic.LeftLeg.Concat(Limbic.RightLeg);
        return ExecuteMotionWithSafety("ExecuteSupervisedWalk", lowerBodyMotors, () =>
        {
            VerifyLowerBus("ExecuteSupervisedWalk");
            _walkController.RequireSupportFootContact = requireSupportFootContact;
            bool success = _walkController.ExecuteWalkCycleSupervised(cycles, stepDurationMs, interpolationSteps, timeoutMs);
            return success ? "Supervised walk completed." : "Supervised walk aborted by safety checks.";
        });
    }

    public string EmergencyStopLower()
    {
        return ExecuteOnBus("EmergencyStop", () =>
        {
            VerifyLowerBus("EmergencyStop");
            _motorControl.SetTorqueOff("lower");
            EnsureLastTxRxSuccessOnLower("EmergencyStop");
            return "Emergency stop applied: lower-body torque disabled.";
        });
    }

    public IReadOnlyList<MotorMonitorReading> ReadMotorMonitoringSnapshot(int overloadThreshold)
    {
        return ExecuteOnBus("ReadMotorMonitoringSnapshot", () =>
        {
            List<MotorMonitorReading> snapshot = new(Motor.MotorContext.Count);
            foreach ((string motorName, byte id) in Motor.MotorContext.OrderBy(kv => kv.Value))
            {
                string location = Motor.ReturnLocation(motorName);
                int port = location == "upper" ? MotorFunctions.PortNumberUpper : MotorFunctions.PortNumberLower;
                try
                {
                    ushort rawLoad = _motorControl.GetPresentLoad(motorName);
                    ushort normalizedLoad = NormalizeLoad(rawLoad);
                    bool torqueOn = ReadTorqueStatus(port, id);
                    int txRxResult = Dynamixel.getLastTxRxResult(port, MotorFunctions.ProtocolVersion);
                    byte packetError = Dynamixel.getLastRxPacketError(port, MotorFunctions.ProtocolVersion);
                    bool communicationOk = txRxResult == MotorFunctions.ComSuccess && packetError == 0;
                    int effectiveThreshold = ResolveOverloadThreshold(motorName, overloadThreshold);
                    bool overload = communicationOk && torqueOn && normalizedLoad >= effectiveThreshold;

                    snapshot.Add(new MotorMonitorReading(
                        motorName,
                        id,
                        location,
                        torqueOn,
                        normalizedLoad,
                        overload,
                        communicationOk,
                        communicationOk ? string.Empty : BuildTxRxDetail(txRxResult, packetError)));
                }
                catch (Exception ex)
                {
                    snapshot.Add(new MotorMonitorReading(
                        motorName,
                        id,
                        location,
                        false,
                        0,
                        false,
                        false,
                        ex.Message));
                }
            }

            return snapshot;
        });
    }

    static ushort NormalizeLoad(ushort rawLoad)
    {
        return (ushort)(rawLoad & 1023);
    }

    static bool ReadTorqueStatus(int port, byte id)
    {
        byte torqueValue = Dynamixel.read1ByteTxRx(
            port,
            MotorFunctions.ProtocolVersion,
            id,
            (ushort)MotorFunctions.MxAddress);
        return torqueValue == MotorFunctions.TorqueEnable;
    }

    static string BuildTxRxDetail(int txRxResult, byte packetError)
    {
        string txRxText = Marshal.PtrToStringAnsi(Dynamixel.getTxRxResult(MotorFunctions.ProtocolVersion, txRxResult)) ?? $"code {txRxResult}";
        if (packetError == 0)
            return txRxText;

        string packetErrorText = Marshal.PtrToStringAnsi(Dynamixel.getRxPacketError(MotorFunctions.ProtocolVersion, packetError)) ?? $"packet_error {packetError}";
        return $"{txRxText}; {packetErrorText}";
    }

    int ResolveOverloadThreshold(string motorName, int fallbackThreshold)
    {
        if (_motorOverloadThresholds.TryGetValue(motorName, out int threshold) && threshold > 0)
            return threshold;
        return fallbackThreshold;
    }

    void LoadMotorOverloadThresholdPolicy()
    {
        EnsureMaps();
        _motorOverloadThresholds.Clear();

        string policyPath = ResolveThresholdPolicyPath();
        if (string.IsNullOrWhiteSpace(policyPath) || !File.Exists(policyPath))
            return;

        try
        {
            string json = File.ReadAllText(policyPath);
            MotorOverloadThresholdPolicy policy = JsonSerializer.Deserialize<MotorOverloadThresholdPolicy>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (policy == null)
                return;

            if (policy.DefaultThreshold > 0)
                _safetyOverloadThreshold = policy.DefaultThreshold;

            if (policy.Motors == null || policy.Motors.Count == 0)
                return;

            foreach ((string motorName, int threshold) in policy.Motors)
            {
                if (threshold <= 0)
                    continue;
                if (Motor.MotorContext == null || !Motor.MotorContext.ContainsKey(motorName))
                    continue;
                _motorOverloadThresholds[motorName] = threshold;
            }
        }
        catch
        {
            // Keep defaults if the policy file is missing/invalid.
        }
    }

    static string ResolveThresholdPolicyPath()
    {
        string runtimeCopy = Path.Combine(AppContext.BaseDirectory, "config", "motor-overload-thresholds.json");
        if (File.Exists(runtimeCopy))
            return runtimeCopy;

        string workspacePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "joi-gtk", "config", "motor-overload-thresholds.json"));
        if (File.Exists(workspacePath))
            return workspacePath;

        return string.Empty;
    }

    static void EnsureNativeDynamixelPrerequisites()
    {
        string nativeFile = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "libdxl_arm64_c.so",
            Architecture.X64 => "libdxl_x64_c.so",
            _ => throw new PlatformNotSupportedException(
                $"Unsupported process architecture for Dynamixel library: {RuntimeInformation.ProcessArchitecture}.")
        };

        string nativePath = Path.Combine(AppContext.BaseDirectory, "lib", nativeFile);
        if (!File.Exists(nativePath))
            throw new FileNotFoundException(
                $"Missing native Dynamixel library at '{nativePath}'. Rebuild joi-gtk so lib/{nativeFile} is copied.",
                nativePath);
    }

    static void EnsureMaps()
    {
        if (Motor.MotorContext == null || Motor.MotorContext.Count == 0)
            MotorFunctions.CollateMotorArray();
    }

    T ExecuteOnBus<T>(string actionName, Func<T> operation)
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized(actionName);
        EnsureMaps();
        lock (_busIoGate)
        {
            return operation();
        }
    }

    T ExecuteMotionWithSafety<T>(string actionName, IEnumerable<string> involvedMotors, Func<T> operation)
    {
        return ExecuteOnBus(actionName, () =>
        {
            string[] scopedMotors = NormalizeMotorList(involvedMotors);
            ValidateSafetyState(actionName, scopedMotors, "pre-check");
            try
            {
                T result = operation();
                ValidateSafetyState(actionName, scopedMotors, "post-check");
                return result;
            }
            catch (Exception ex) when (!IsSafetyGateError(ex))
            {
                string failSafeResult = ApplyFailSafeTorqueOff(scopedMotors);
                AppendSafetyEvent(
                    eventType: "MOTION_EXCEPTION",
                    actionName: actionName,
                    phase: "runtime",
                    scopedMotors: scopedMotors,
                    detail: ex.Message,
                    failSafeAction: failSafeResult);
                throw;
            }
        });
    }

    static bool IsSafetyGateError(Exception ex)
    {
        return ex is InvalidOperationException &&
               ex.Message.StartsWith("SafetyGate", StringComparison.Ordinal);
    }

    static string[] NormalizeMotorList(IEnumerable<string> involvedMotors)
    {
        if (involvedMotors == null)
            return Array.Empty<string>();

        return involvedMotors
            .Where(motor => !string.IsNullOrWhiteSpace(motor))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    void ValidateSafetyState(string actionName, string[] scopedMotors, string phase)
    {
        IReadOnlyList<MotorMonitorReading> snapshot = ReadMotorMonitoringSnapshot(_safetyOverloadThreshold);
        bool filterByScope = scopedMotors.Length > 0;
        HashSet<string> scope = filterByScope
            ? new HashSet<string>(scopedMotors, StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        MotorMonitorReading[] scopedSnapshot = filterByScope
            ? snapshot.Where(reading => scope.Contains(reading.MotorName)).ToArray()
            : snapshot.ToArray();

        MotorMonitorReading[] communicationErrors = scopedSnapshot
            .Where(reading => !reading.CommunicationOk)
            .ToArray();
        MotorMonitorReading[] overloads = scopedSnapshot
            .Where(reading => reading.Overload)
            .ToArray();
        MotorMonitorReading[] torqueOff = scopedSnapshot
            .Where(reading => !reading.TorqueOn)
            .ToArray();

        if (communicationErrors.Length == 0 && overloads.Length == 0 && torqueOff.Length == 0)
            return;

        string detail =
            $"Comm={FormatSafetyRows(communicationErrors, row => $"{row.MotorName}:{row.Error}")}; " +
            $"Overload={FormatSafetyRows(overloads, row => $"{row.MotorName}:{row.Load}")}; " +
            $"TorqueOff={FormatSafetyRows(torqueOff, row => row.MotorName)}.";

        string failSafeResult = ApplyFailSafeTorqueOff(scopedMotors);
        SafetyGateTripped?.Invoke(new SafetyGateTrip(actionName, phase, scopedMotors, detail));
        AppendSafetyEvent(
            eventType: "SAFETY_GATE_TRIP",
            actionName: actionName,
            phase: phase,
            scopedMotors: scopedMotors,
            detail: detail,
            failSafeAction: failSafeResult);
        throw new InvalidOperationException(
            $"SafetyGate blocked {actionName} ({phase}). {detail}");
    }

    static string FormatSafetyRows(IReadOnlyList<MotorMonitorReading> rows, Func<MotorMonitorReading, string> formatter)
    {
        if (rows.Count == 0)
            return "none";

        return string.Join(", ", rows.Select(formatter));
    }

    string ApplyFailSafeTorqueOff(string[] scopedMotors)
    {
        try
        {
            if (scopedMotors.Length > 0)
            {
                _motorControl.SetTorqueOff(scopedMotors);
                return $"torque_off[{string.Join(",", scopedMotors)}]";
            }
            else
            {
                _motorControl.SetTorqueOff("lower");
                return "torque_off[lower]";
            }
        }
        catch
        {
            return "torque_off_failed";
        }
    }

    void AppendSafetyEvent(
        string eventType,
        string actionName,
        string phase,
        IReadOnlyList<string> scopedMotors,
        string detail,
        string failSafeAction)
    {
        try
        {
            string scope = scopedMotors == null || scopedMotors.Count == 0
                ? "all"
                : string.Join(",", scopedMotors);
            string line =
                $"{DateTimeOffset.UtcNow:O}\tevent={eventType}\taction={actionName}\tphase={phase}\tscope={scope}\tfailsafe={failSafeAction}\tdetail={detail}";
            string directory = Path.GetDirectoryName(_safetyEventLogPath) ?? string.Empty;
            if (directory.Length > 0)
                Directory.CreateDirectory(directory);
            File.AppendAllText(_safetyEventLogPath, line + Environment.NewLine);
        }
        catch
        {
            // Logging must never block robot safety flow.
        }
    }

    static string ResolveSafetyEventLogPath()
    {
        string runtimePath = Path.Combine(AppContext.BaseDirectory, "logs", "safety-events.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(runtimePath) ?? "logs");
            return runtimePath;
        }
        catch
        {
            string workspacePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "logs", "safety-events.log"));
            return workspacePath;
        }
    }

    static void AutoConfigureLinuxBusMapping()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        string existingUpper = Environment.GetEnvironmentVariable("ARTHUR_UPPER_PORT") ?? string.Empty;
        string existingLower = Environment.GetEnvironmentVariable("ARTHUR_LOWER_PORT") ?? string.Empty;
        bool hasUpper = !string.IsNullOrWhiteSpace(existingUpper);
        bool hasLower = !string.IsNullOrWhiteSpace(existingLower);
        if (hasUpper && hasLower)
            return;

        string[] candidates = Directory
            .GetFiles("/dev", "ttyUSB*")
            .Concat(Directory.GetFiles("/dev", "ttyACM*"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length < 2)
            return;

        if (!hasUpper)
            Environment.SetEnvironmentVariable("ARTHUR_UPPER_PORT", candidates[0]);

        if (!hasLower)
        {
            string lowerCandidate = candidates[1];
            string upperAssigned = Environment.GetEnvironmentVariable("ARTHUR_UPPER_PORT") ?? string.Empty;
            if (string.Equals(lowerCandidate, upperAssigned, StringComparison.Ordinal) && candidates.Length > 2)
                lowerCandidate = candidates[2];
            Environment.SetEnvironmentVariable("ARTHUR_LOWER_PORT", lowerCandidate);
        }
    }

    void EnsureInitialized(string actionName)
    {
        if (!_initialized)
            throw new InvalidOperationException($"{actionName} requires successful initialization with an attached robot.");
    }

    void VerifyLowerBus(string actionName)
    {
        // Probe one known lower-body motor ID and verify SDK Tx/Rx status.
        _motorControl.GetPresentPosition("r_hip_x");
        EnsureLastTxRxSuccessOnLower(actionName);
    }

    int ScanLowerMotors()
    {
        MotorFunctions.SetBaudRate("lower");
        int successCount = 0;

        foreach (byte id in Limbic.LeftLeg.Concat(Limbic.RightLeg).Select(Motor.ReturnID).Distinct())
        {
            Dynamixel.ping(MotorFunctions.PortNumberLower, MotorFunctions.ProtocolVersion, id);
            int txRxResult = Dynamixel.getLastTxRxResult(MotorFunctions.PortNumberLower, MotorFunctions.ProtocolVersion);
            byte packetError = Dynamixel.getLastRxPacketError(MotorFunctions.PortNumberLower, MotorFunctions.ProtocolVersion);
            if (txRxResult == MotorFunctions.ComSuccess && packetError == 0)
                successCount++;
        }

        if (successCount == 0)
            throw new InvalidOperationException("Initialize failed: no lower-body motors responded to scan on the lower bus.");

        return successCount;
    }

    static void EnsureLastTxRxSuccessOnLower(string actionName)
    {
        int result = Dynamixel.getLastTxRxResult(MotorFunctions.PortNumberLower, MotorFunctions.ProtocolVersion);
        if (result == MotorFunctions.ComSuccess)
            return;

        string detail = Marshal.PtrToStringAnsi(Dynamixel.getTxRxResult(MotorFunctions.ProtocolVersion, result)) ?? $"code {result}";
        throw new InvalidOperationException($"{actionName} failed: lower bus communication error ({detail}).");
    }
}

sealed class MotorOverloadThresholdPolicy
{
    public int DefaultThreshold { get; set; }
    public Dictionary<string, int> Motors { get; set; } = new(StringComparer.Ordinal);
}

public sealed class SafetyGateTrip
{
    public SafetyGateTrip(string actionName, string phase, IReadOnlyList<string> scope, string detail)
    {
        ActionName = actionName;
        Phase = phase;
        Scope = scope;
        Detail = detail;
    }

    public string ActionName { get; }
    public string Phase { get; }
    public IReadOnlyList<string> Scope { get; }
    public string Detail { get; }
}

public sealed class MotorMonitorReading
{
    public MotorMonitorReading(
        string motorName,
        byte id,
        string location,
        bool torqueOn,
        ushort load,
        bool overload,
        bool communicationOk,
        string error)
    {
        MotorName = motorName;
        ID = id;
        Location = location;
        TorqueOn = torqueOn;
        Load = load;
        Overload = overload;
        CommunicationOk = communicationOk;
        Error = error;
    }

    public string MotorName { get; }
    public byte ID { get; }
    public string Location { get; }
    public bool TorqueOn { get; }
    public ushort Load { get; }
    public bool Overload { get; }
    public bool CommunicationOk { get; }
    public string Error { get; }
}
