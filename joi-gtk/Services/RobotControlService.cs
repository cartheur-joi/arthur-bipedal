using Cartheur.Animals.Robot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace joi_gtk.Services;

public sealed class RobotControlService
{
    readonly MotorFunctions _motorControl;
    readonly WalkController _walkController;
    bool _initialized;

    public RobotControlService()
    {
        _motorControl = new MotorFunctions();
        _walkController = new WalkController(_motorControl);
    }

    public string Initialize()
    {
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

    public string TorqueOnLower()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("TorqueOnLower");
        EnsureMaps();
        VerifyLowerBus("TorqueOnLower");
        _motorControl.SetTorqueOn("lower");
        EnsureLastTxRxSuccessOnLower("TorqueOnLower");
        return "Lower-body torque enabled.";
    }

    public string TorqueOffLower()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("TorqueOffLower");
        EnsureMaps();
        VerifyLowerBus("TorqueOffLower");
        _motorControl.SetTorqueOff("lower");
        EnsureLastTxRxSuccessOnLower("TorqueOffLower");
        return "Lower-body torque disabled.";
    }

    public string ReadLowerTelemetry()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("ReadLowerTelemetry");
        EnsureMaps();
        VerifyLowerBus("ReadLowerTelemetry");
        Dictionary<string, int> snapshot = _motorControl.GetPresentPositions(Limbic.LeftLeg);
        foreach (KeyValuePair<string, int> kv in _motorControl.GetPresentPositions(Limbic.RightLeg))
            snapshot[kv.Key] = kv.Value;

        return "Lower pose: " + string.Join(", ", snapshot.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public Dictionary<string, int> ReadPositions(string[] motors)
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("ReadPositions");
        EnsureMaps();
        if (motors == null || motors.Length == 0)
            throw new InvalidOperationException("ReadPositions requires one or more motors.");

        return _motorControl.GetPresentPositions(motors);
    }

    public void SetTorqueOff(string[] motors)
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("SetTorqueOff");
        EnsureMaps();
        if (motors == null || motors.Length == 0)
            throw new InvalidOperationException("SetTorqueOff requires one or more motors.");

        _motorControl.SetTorqueOff(motors);
    }

    public void SetTorqueOn(string[] motors)
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("SetTorqueOn");
        EnsureMaps();
        if (motors == null || motors.Length == 0)
            throw new InvalidOperationException("SetTorqueOn requires one or more motors.");

        _motorControl.SetTorqueOn(motors);
    }

    public void MoveToPositions(Dictionary<string, int> targets, int durationMilliseconds = 700, int interpolationSteps = 6)
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("MoveToPositions");
        EnsureMaps();
        if (targets == null || targets.Count == 0)
            throw new InvalidOperationException("MoveToPositions requires one or more target motors.");

        _motorControl.MoveMotorSequenceSmooth(targets, durationMilliseconds, interpolationSteps);
    }

    public string ExecuteWalkCycleSupervised(int cycles, int stepDurationMs, int interpolationSteps, int timeoutMs, bool requireSupportFootContact)
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("ExecuteSupervisedWalk");
        EnsureMaps();
        VerifyLowerBus("ExecuteSupervisedWalk");
        _walkController.RequireSupportFootContact = requireSupportFootContact;
        bool success = _walkController.ExecuteWalkCycleSupervised(cycles, stepDurationMs, interpolationSteps, timeoutMs);
        return success ? "Supervised walk completed." : "Supervised walk aborted by safety checks.";
    }

    public string EmergencyStopLower()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("EmergencyStop");
        EnsureMaps();
        VerifyLowerBus("EmergencyStop");
        _motorControl.SetTorqueOff("lower");
        EnsureLastTxRxSuccessOnLower("EmergencyStop");
        return "Emergency stop applied: lower-body torque disabled.";
    }

    public IReadOnlyList<MotorMonitorReading> ReadMotorMonitoringSnapshot(int overloadThreshold)
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureInitialized("ReadMotorMonitoringSnapshot");
        EnsureMaps();

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
                bool overload = communicationOk && torqueOn && normalizedLoad >= overloadThreshold;

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
