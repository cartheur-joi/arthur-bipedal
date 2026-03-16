using Cartheur.Animals.Robot;
using Microsoft.Data.Sqlite;
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
    readonly IImuProvider _imuProvider;
    readonly object _initializeGate = new();
    readonly object _busIoGate = new();
    readonly Dictionary<string, int> _motorOverloadThresholds = new(StringComparer.Ordinal);
    readonly Dictionary<string, int> _motorMaxTemperatures = new(StringComparer.Ordinal);
    readonly Dictionary<string, int> _motorMinVoltages = new(StringComparer.Ordinal);
    readonly string _safetyEventLogPath;
    readonly string _stableSittingPositionPath;
    readonly BodyModelPolicy _bodyModelPolicy;
    readonly string _bodyCalibrationReportPath;
    int _safetyOverloadThreshold = MotorFunctions.PresentLoadAlarm;
    int _safetyMaxTemperature = 70;
    int _safetyMinVoltage = 90;
    bool _initialized;
    bool _stableSittingPositionCaptured;
    public event Action<SafetyGateTrip> SafetyGateTripped;

    public RobotControlService()
    {
        _motorControl = new MotorFunctions();
        _imuProvider = BuildImuProvider();
        _walkController = new WalkController(_motorControl, _imuProvider);
        LoadMotorSafetyThresholdPolicy();
        _safetyEventLogPath = ResolveSafetyEventLogPath();
        _stableSittingPositionPath = ResolveStableSittingPositionPath();
        _bodyModelPolicy = LoadBodyModelPolicy();
        _bodyCalibrationReportPath = ResolveBodyCalibrationReportPath();
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
    public bool StableSittingPositionCaptured => _stableSittingPositionCaptured;
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

    public string CaptureStableSittingPosition()
    {
        const string positionTag = "Stable Sitting Position";
        return ExecuteOnBus("CaptureStableSittingPosition", () =>
        {
            EnsureMaps();
            Dictionary<string, int> snapshot = _motorControl.GetPresentPositions(Limbic.All);
            if (snapshot == null || snapshot.Count == 0)
                throw new InvalidOperationException("No present motor positions were available to capture.");

            PersistStableSittingPosition(snapshot, positionTag);

            _stableSittingPositionCaptured = true;
            return $"Captured {snapshot.Count} motors and stored as '{positionTag}' (flag=true).";
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

    public string ReadImuTelemetry()
    {
        ImuSample sample = _imuProvider.GetSample();
        string source = _imuProvider is SerialMpuImuProvider serial
            ? serial.Status
            : "provider=null";
        if (!sample.IsValid)
            return $"IMU sample unavailable ({source}).";

        return
            $"IMU pitch={sample.PitchDegrees:0.00} roll={sample.RollDegrees:0.00} yaw={sample.YawDegrees:0.00} ({source}).";
    }

    public string ApplyStandingBalanceCompensationStep()
    {
        string[] compensationMotors = { "l_ankle_y", "r_ankle_y", "l_hip_x", "r_hip_x" };
        return ExecuteMotionWithSafety("BalanceCompensation", compensationMotors, () =>
        {
            ImuSample sample = _imuProvider.GetSample();
            if (!sample.IsValid)
                return "Balance compensation skipped: IMU sample unavailable.";

            const double deadband = 1.5;
            int pitchCommand = Math.Abs(sample.PitchDegrees) < deadband
                ? 0
                : Clamp((int)Math.Round(sample.PitchDegrees * 1.6), -20, 20);
            int rollCommand = Math.Abs(sample.RollDegrees) < deadband
                ? 0
                : Clamp((int)Math.Round(sample.RollDegrees * 1.4), -20, 20);

            if (pitchCommand == 0 && rollCommand == 0)
                return $"Balance stable (pitch={sample.PitchDegrees:0.00}, roll={sample.RollDegrees:0.00}).";

            Dictionary<string, int> current = _motorControl.GetPresentPositions(compensationMotors);
            int lAnkle = ClampPosition(current["l_ankle_y"] - pitchCommand);
            int rAnkle = ClampPosition(current["r_ankle_y"] - pitchCommand);
            int lHip = ClampPosition(current["l_hip_x"] + rollCommand);
            int rHip = ClampPosition(current["r_hip_x"] - rollCommand);

            Dictionary<string, int> targets = new()
            {
                ["l_ankle_y"] = lAnkle,
                ["r_ankle_y"] = rAnkle,
                ["l_hip_x"] = lHip,
                ["r_hip_x"] = rHip
            };
            _motorControl.MoveMotorSequenceSmooth(targets, 280, 5);

            return
                $"Balance compensation applied: pitch={sample.PitchDegrees:0.00}, roll={sample.RollDegrees:0.00}, " +
                $"cmdPitch={pitchCommand}, cmdRoll={rollCommand}.";
        });
    }

    public string ExecuteSeatedHandshakeSafetyTest(int shakes = 3, int stepDurationMs = 450, int interpolationSteps = 8)
    {
        if (shakes < 1 || shakes > 8)
            throw new ArgumentOutOfRangeException(nameof(shakes), "Handshake shake count must be between 1 and 8.");
        if (stepDurationMs < 220)
            throw new ArgumentOutOfRangeException(nameof(stepDurationMs), "Handshake step duration must be >= 220 ms.");
        if (interpolationSteps < 3 || interpolationSteps > 30)
            throw new ArgumentOutOfRangeException(nameof(interpolationSteps), "Handshake interpolation steps must be between 3 and 30.");

        string[] armMotors = Limbic.RightArm.ToArray();
        return ExecuteOnBus("SeatedHandshakeSafetyTest", () =>
        {
            _motorControl.SetTorqueOn(armMotors);
            try
            {
                return ExecuteMotionWithSafety("SeatedHandshakeSafetyTest", armMotors, () =>
                {
                    Dictionary<string, int> origin = _motorControl.GetPresentPositions(armMotors);

                    int shoulderLift = ClampArmDelta(origin["r_shoulder_y"], +55);
                    int shoulderForward = ClampArmDelta(origin["r_shoulder_x"], -70);
                    int shoulderWaveOut = ClampArmDelta(shoulderForward, -18);
                    int shoulderWaveIn = ClampArmDelta(shoulderForward, +18);
                    int forearmPresent = ClampArmDelta(origin["r_arm_z"], +55);
                    int elbowBend = ClampArmDelta(origin["r_elbow_y"], -95);
                    int elbowExtend = ClampArmDelta(origin["r_elbow_y"], -30);

                    Dictionary<string, int> engagePose = BuildHandshakePose(
                        origin,
                        shoulderY: shoulderLift,
                        shoulderX: shoulderForward,
                        armZ: forearmPresent,
                        elbowY: elbowBend);
                    Dictionary<string, int> shakeInPose = BuildHandshakePose(
                        origin,
                        shoulderY: shoulderLift,
                        shoulderX: shoulderWaveIn,
                        armZ: forearmPresent,
                        elbowY: elbowBend);
                    Dictionary<string, int> shakeOutPose = BuildHandshakePose(
                        origin,
                        shoulderY: shoulderLift,
                        shoulderX: shoulderWaveOut,
                        armZ: forearmPresent,
                        elbowY: elbowExtend);

                    try
                    {
                        _motorControl.MoveMotorSequenceSmooth(engagePose, stepDurationMs, interpolationSteps);
                        for (int i = 0; i < shakes; i++)
                        {
                            _motorControl.MoveMotorSequenceSmooth(shakeOutPose, stepDurationMs, interpolationSteps);
                            _motorControl.MoveMotorSequenceSmooth(shakeInPose, stepDurationMs, interpolationSteps);
                        }

                        Dictionary<string, int> lowerPose = BuildHandshakePose(
                            origin,
                            shoulderY: origin["r_shoulder_y"],
                            shoulderX: origin["r_shoulder_x"],
                            armZ: origin["r_arm_z"],
                            elbowY: elbowExtend);
                        _motorControl.MoveMotorSequenceSmooth(lowerPose, stepDurationMs, interpolationSteps);
                    }
                    finally
                    {
                        // Always attempt to return to observed origin pose for seated tests.
                        _motorControl.MoveMotorSequenceSmooth(origin, stepDurationMs, interpolationSteps);
                    }

                    return $"Seated handshake completed with {shakes} shake cycles (shoulder+forearm+elbow) and origin return.";
                });
            }
            finally
            {
                _motorControl.SetTorqueOff(armMotors);
            }
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

    public string RunStartupBodyAwarenessCalibration(bool strict = true)
    {
        return ExecuteOnBus("BodyAwarenessCalibration", () =>
        {
            BodyModelPolicy policy = _bodyModelPolicy;
            if (policy == null || policy.Joints == null || policy.Joints.Count == 0)
                throw new InvalidOperationException("Body model policy is not available. Check config/body-model.json.");

            List<BodyJointCalibrationSample> samples = new();
            List<string> missingJoints = new();

            foreach ((string jointName, BodyJointPolicy jointPolicy) in policy.Joints.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (Motor.MotorContext == null || !Motor.MotorContext.ContainsKey(jointName))
                {
                    missingJoints.Add(jointName);
                    continue;
                }

                ResolvedJointLimits limits = ResolveJointLimits(policy, jointPolicy);
                ushort position = _motorControl.GetPresentPosition(jointName);
                ushort load = NormalizeLoad(_motorControl.GetPresentLoad(jointName));
                ushort temperature = _motorControl.GetPresentTemperture(jointName);
                ushort voltage = _motorControl.GetPresentVoltage(jointName);

                bool withinHardRange = position >= limits.HardMin && position <= limits.HardMax;
                bool withinSoftRange = position >= limits.SoftMin && position <= limits.SoftMax;
                double softRangeProgress = limits.SoftMax > limits.SoftMin
                    ? (position - limits.SoftMin) / (double)(limits.SoftMax - limits.SoftMin)
                    : 0.0;

                samples.Add(new BodyJointCalibrationSample(
                    jointName,
                    jointPolicy.Parent ?? string.Empty,
                    jointPolicy.Axis ?? string.Empty,
                    jointPolicy.Location ?? Motor.ReturnLocation(jointName),
                    position,
                    load,
                    temperature,
                    voltage,
                    limits.HardMin,
                    limits.HardMax,
                    limits.SoftMin,
                    limits.SoftMax,
                    withinHardRange,
                    withinSoftRange,
                    softRangeProgress));
            }

            BodyCalibrationReport report = new(
                DateTimeOffset.UtcNow,
                policy.ModelVersion ?? "unknown",
                policy.RootJoint ?? string.Empty,
                strict,
                samples,
                missingJoints);

            PersistBodyCalibrationReport(report);

            int hardViolations = samples.Count(s => !s.WithinHardRange);
            int softWarnings = samples.Count(s => !s.WithinSoftRange);
            if (strict && (hardViolations > 0 || missingJoints.Count > 0))
            {
                ApplyCalibrationFailSafeTorqueOff();
                throw new InvalidOperationException(
                    $"Body awareness calibration failed (strict): hard_violations={hardViolations}, missing_joints={missingJoints.Count}. " +
                    $"Report={_bodyCalibrationReportPath}");
            }

            return
                $"Body calibration completed: joints={samples.Count}, hard_violations={hardViolations}, " +
                $"soft_warnings={softWarnings}, missing={missingJoints.Count}, report={_bodyCalibrationReportPath}.";
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
                    ushort temperature = _motorControl.GetPresentTemperture(motorName);
                    ushort voltage = _motorControl.GetPresentVoltage(motorName);
                    bool torqueOn = ReadTorqueStatus(port, id);
                    int txRxResult = Dynamixel.getLastTxRxResult(port, MotorFunctions.ProtocolVersion);
                    byte packetError = Dynamixel.getLastRxPacketError(port, MotorFunctions.ProtocolVersion);
                    bool communicationOk = txRxResult == MotorFunctions.ComSuccess && packetError == 0;
                    int effectiveOverloadThreshold = ResolveOverloadThreshold(motorName, overloadThreshold);
                    int effectiveMaxTemperature = ResolveMaxTemperature(motorName);
                    int effectiveMinVoltage = ResolveMinVoltage(motorName);
                    bool overload = communicationOk && torqueOn && normalizedLoad >= effectiveOverloadThreshold;
                    bool thermalViolation = communicationOk && torqueOn && temperature >= effectiveMaxTemperature;
                    bool voltageViolation = communicationOk && torqueOn && voltage <= effectiveMinVoltage;

                    snapshot.Add(new MotorMonitorReading(
                        motorName,
                        id,
                        location,
                        torqueOn,
                        normalizedLoad,
                        temperature,
                        voltage,
                        overload,
                        thermalViolation,
                        voltageViolation,
                        communicationOk,
                        communicationOk ? string.Empty : BuildTxRxDetail(txRxResult, packetError),
                        effectiveOverloadThreshold,
                        effectiveMaxTemperature,
                        effectiveMinVoltage));
                }
                catch (Exception ex)
                {
                    snapshot.Add(new MotorMonitorReading(
                        motorName,
                        id,
                        location,
                        false,
                        0,
                        0,
                        0,
                        false,
                        false,
                        false,
                        false,
                        ex.Message,
                        ResolveOverloadThreshold(motorName, overloadThreshold),
                        ResolveMaxTemperature(motorName),
                        ResolveMinVoltage(motorName)));
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

    int ResolveMaxTemperature(string motorName)
    {
        if (_motorMaxTemperatures.TryGetValue(motorName, out int maxTemperature) && maxTemperature > 0)
            return maxTemperature;
        return _safetyMaxTemperature;
    }

    int ResolveMinVoltage(string motorName)
    {
        if (_motorMinVoltages.TryGetValue(motorName, out int minVoltage) && minVoltage > 0)
            return minVoltage;
        return _safetyMinVoltage;
    }

    void LoadMotorSafetyThresholdPolicy()
    {
        EnsureMaps();
        _motorOverloadThresholds.Clear();
        _motorMaxTemperatures.Clear();
        _motorMinVoltages.Clear();

        string policyPath = ResolveSafetyPolicyPath();
        if (string.IsNullOrWhiteSpace(policyPath) || !File.Exists(policyPath))
            return;

        try
        {
            string json = File.ReadAllText(policyPath);
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return;

            if (TryReadPositiveInt(root, out int defaultThreshold, "defaultThreshold"))
                _safetyOverloadThreshold = defaultThreshold;
            if (TryReadPositiveInt(root, out int defaultMaxTemperature, "defaultMaxTemperature"))
                _safetyMaxTemperature = defaultMaxTemperature;
            if (TryReadPositiveInt(root, out int defaultMinVoltage, "defaultMinVoltage"))
                _safetyMinVoltage = defaultMinVoltage;

            if (root.TryGetProperty("defaults", out JsonElement defaults) && defaults.ValueKind == JsonValueKind.Object)
            {
                if (TryReadPositiveInt(defaults, out int nestedThreshold, "overloadThreshold", "defaultThreshold"))
                    _safetyOverloadThreshold = nestedThreshold;
                if (TryReadPositiveInt(defaults, out int nestedMaxTemperature, "maxTemperature", "defaultMaxTemperature"))
                    _safetyMaxTemperature = nestedMaxTemperature;
                if (TryReadPositiveInt(defaults, out int nestedMinVoltage, "minVoltage", "defaultMinVoltage"))
                    _safetyMinVoltage = nestedMinVoltage;
            }

            if (!root.TryGetProperty("motors", out JsonElement motors) || motors.ValueKind != JsonValueKind.Object)
                return;

            foreach (JsonProperty motorPolicy in motors.EnumerateObject())
            {
                string motorName = motorPolicy.Name;
                if (Motor.MotorContext == null || !Motor.MotorContext.ContainsKey(motorName))
                    continue;

                JsonElement value = motorPolicy.Value;
                if (value.ValueKind == JsonValueKind.Number)
                {
                    if (value.TryGetInt32(out int threshold) && threshold > 0)
                        _motorOverloadThresholds[motorName] = threshold;
                    continue;
                }

                if (value.ValueKind != JsonValueKind.Object)
                    continue;

                if (TryReadPositiveInt(value, out int overloadThreshold, "overloadThreshold", "threshold"))
                    _motorOverloadThresholds[motorName] = overloadThreshold;
                if (TryReadPositiveInt(value, out int maxTemperature, "maxTemperature"))
                    _motorMaxTemperatures[motorName] = maxTemperature;
                if (TryReadPositiveInt(value, out int minVoltage, "minVoltage"))
                    _motorMinVoltages[motorName] = minVoltage;
            }
        }
        catch
        {
            // Keep defaults if the policy file is missing/invalid.
        }
    }

    static bool TryReadPositiveInt(JsonElement source, out int value, params string[] propertyNames)
    {
        value = 0;
        foreach (string propertyName in propertyNames)
        {
            if (!source.TryGetProperty(propertyName, out JsonElement candidate))
                continue;

            if (!candidate.TryGetInt32(out int parsed) || parsed <= 0)
                continue;

            value = parsed;
            return true;
        }

        return false;
    }

    static string ResolveSafetyPolicyPath()
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
        MotorMonitorReading[] thermalViolations = scopedSnapshot
            .Where(reading => reading.ThermalViolation)
            .ToArray();
        MotorMonitorReading[] voltageViolations = scopedSnapshot
            .Where(reading => reading.VoltageViolation)
            .ToArray();
        MotorMonitorReading[] torqueOff = scopedSnapshot
            .Where(reading => !reading.TorqueOn)
            .ToArray();

        if (communicationErrors.Length == 0 &&
            overloads.Length == 0 &&
            thermalViolations.Length == 0 &&
            voltageViolations.Length == 0 &&
            torqueOff.Length == 0)
            return;

        string detail =
            $"Comm={FormatSafetyRows(communicationErrors, row => $"{row.MotorName}:{row.Error}")}; " +
            $"Overload={FormatSafetyRows(overloads, row => $"{row.MotorName}:{row.Load}>={row.OverloadThreshold}")}; " +
            $"Thermal={FormatSafetyRows(thermalViolations, row => $"{row.MotorName}:{row.Temperature}>={row.MaxTemperature}")}; " +
            $"Voltage={FormatSafetyRows(voltageViolations, row => $"{row.MotorName}:{row.Voltage}<={row.MinVoltage}")}; " +
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

    static string ResolveStableSittingPositionPath()
    {
        string runtimePath = Path.Combine(AppContext.BaseDirectory, "db", "positions.db");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(runtimePath) ?? "db");
            return runtimePath;
        }
        catch
        {
            return Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "db", "positions.db"));
        }
    }

    void PersistStableSittingPosition(IReadOnlyDictionary<string, int> snapshot, string positionTag)
    {
        if (snapshot == null || snapshot.Count == 0)
            throw new InvalidOperationException("Cannot store empty stable sitting snapshot.");

        using SqliteConnection connection = new($"Data Source={_stableSittingPositionPath}");
        connection.Open();

        using SqliteTransaction tx = connection.BeginTransaction();
        EnsurePoseSnapshotSchema(connection, tx);
        long snapshotId = InsertPoseSnapshot(connection, tx, positionTag, snapshot.Count);
        foreach ((string motorName, int positionValue) in snapshot.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            using SqliteCommand insert = connection.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText =
                "INSERT INTO pose_snapshot_value (snapshot_id, motor_name, position_value) " +
                "VALUES ($snapshotId, $motorName, $positionValue);";
            insert.Parameters.AddWithValue("$snapshotId", snapshotId);
            insert.Parameters.AddWithValue("$motorName", motorName);
            insert.Parameters.AddWithValue("$positionValue", positionValue);
            insert.ExecuteNonQuery();
        }

        tx.Commit();
    }

    static void EnsurePoseSnapshotSchema(SqliteConnection connection, SqliteTransaction tx)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText =
            "DROP TABLE IF EXISTS StablePosition;" +
            "CREATE TABLE IF NOT EXISTS pose_snapshot (" +
            "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "pose_name TEXT NOT NULL, " +
            "captured_at_utc TEXT NOT NULL, " +
            "motor_count INTEGER NOT NULL, " +
            "note TEXT NULL);" +
            "CREATE TABLE IF NOT EXISTS pose_snapshot_value (" +
            "snapshot_id INTEGER NOT NULL, " +
            "motor_name TEXT NOT NULL, " +
            "position_value INTEGER NOT NULL, " +
            "PRIMARY KEY(snapshot_id, motor_name), " +
            "FOREIGN KEY(snapshot_id) REFERENCES pose_snapshot(id) ON DELETE CASCADE);" +
            "CREATE INDEX IF NOT EXISTS idx_pose_snapshot_name_time " +
            "ON pose_snapshot(pose_name, captured_at_utc DESC);";
        command.ExecuteNonQuery();
    }

    static long InsertPoseSnapshot(
        SqliteConnection connection,
        SqliteTransaction tx,
        string poseName,
        int motorCount)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText =
            "INSERT INTO pose_snapshot (pose_name, captured_at_utc, motor_count, note) " +
            "VALUES ($poseName, $capturedAtUtc, $motorCount, $note); " +
            "SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$poseName", poseName);
        command.Parameters.AddWithValue("$capturedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$motorCount", motorCount);
        command.Parameters.AddWithValue("$note", "Captured from live motors via --capture-stable-sitting.");
        object id = command.ExecuteScalar();
        return Convert.ToInt64(id);
    }

    static BodyModelPolicy LoadBodyModelPolicy()
    {
        string policyPath = ResolveBodyModelPolicyPath();
        if (string.IsNullOrWhiteSpace(policyPath) || !File.Exists(policyPath))
            return new BodyModelPolicy();

        try
        {
            string json = File.ReadAllText(policyPath);
            BodyModelPolicy policy = JsonSerializer.Deserialize<BodyModelPolicy>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return policy ?? new BodyModelPolicy();
        }
        catch
        {
            return new BodyModelPolicy();
        }
    }

    static string ResolveBodyModelPolicyPath()
    {
        string runtimeCopy = Path.Combine(AppContext.BaseDirectory, "config", "body-model.json");
        if (File.Exists(runtimeCopy))
            return runtimeCopy;

        string workspacePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "joi-gtk", "config", "body-model.json"));
        if (File.Exists(workspacePath))
            return workspacePath;

        return string.Empty;
    }

    static string ResolveBodyCalibrationReportPath()
    {
        string runtimePath = Path.Combine(AppContext.BaseDirectory, "logs", "body-awareness-last.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(runtimePath) ?? "logs");
            return runtimePath;
        }
        catch
        {
            return Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "logs", "body-awareness-last.json"));
        }
    }

    ResolvedJointLimits ResolveJointLimits(BodyModelPolicy policy, BodyJointPolicy jointPolicy)
    {
        BodyJointLimits defaults = policy.Defaults ?? new BodyJointLimits();
        BodyJointLimits limits = jointPolicy?.Limits ?? new BodyJointLimits();
        int hardMin = limits.HardMin ?? defaults.HardMin ?? 0;
        int hardMax = limits.HardMax ?? defaults.HardMax ?? 1023;
        int softMin = limits.SoftMin ?? defaults.SoftMin ?? hardMin;
        int softMax = limits.SoftMax ?? defaults.SoftMax ?? hardMax;

        hardMin = Clamp(hardMin, 0, 4095);
        hardMax = Clamp(hardMax, hardMin, 4095);
        softMin = Clamp(softMin, hardMin, hardMax);
        softMax = Clamp(softMax, softMin, hardMax);
        return new ResolvedJointLimits(hardMin, hardMax, softMin, softMax);
    }

    void PersistBodyCalibrationReport(BodyCalibrationReport report)
    {
        try
        {
            string directory = Path.GetDirectoryName(_bodyCalibrationReportPath) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_bodyCalibrationReportPath, json);
        }
        catch
        {
            // Report writing should never block startup safety flow.
        }
    }

    void ApplyCalibrationFailSafeTorqueOff()
    {
        try
        {
            _motorControl.SetTorqueOff("upper");
        }
        catch
        {
            // Best effort.
        }

        try
        {
            _motorControl.SetTorqueOff("lower");
        }
        catch
        {
            // Best effort.
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

    static IImuProvider BuildImuProvider()
    {
        string port = Environment.GetEnvironmentVariable("ARTHUR_IMU_PORT") ?? "/dev/ttyUSB2";
        int baud = 115200;
        string baudRaw = Environment.GetEnvironmentVariable("ARTHUR_IMU_BAUD");
        if (!string.IsNullOrWhiteSpace(baudRaw) && int.TryParse(baudRaw, out int parsedBaud) && parsedBaud > 0)
            baud = parsedBaud;

        return new SerialMpuImuProvider(port, baud);
    }

    static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    static int ClampPosition(int value)
    {
        // Conservative 10-bit guard used by existing gait and standing utilities.
        return Clamp(value, 0, 1023);
    }

    static int ClampArmDelta(int origin, int delta)
    {
        return ClampPosition(origin + delta);
    }

    static Dictionary<string, int> BuildHandshakePose(Dictionary<string, int> origin, int shoulderY, int shoulderX, int armZ, int elbowY)
    {
        return new Dictionary<string, int>
        {
            ["r_shoulder_y"] = shoulderY,
            ["r_shoulder_x"] = shoulderX,
            ["r_arm_z"] = armZ,
            ["r_elbow_y"] = elbowY
        };
    }
}

sealed class BodyModelPolicy
{
    public string ModelVersion { get; set; } = "unknown";
    public string RootJoint { get; set; } = string.Empty;
    public BodyJointLimits Defaults { get; set; } = new();
    public Dictionary<string, BodyJointPolicy> Joints { get; set; } = new(StringComparer.Ordinal);
}

sealed class BodyJointPolicy
{
    public string Parent { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public BodyJointLimits Limits { get; set; } = new();
}

sealed class BodyJointLimits
{
    public int? HardMin { get; set; }
    public int? HardMax { get; set; }
    public int? SoftMin { get; set; }
    public int? SoftMax { get; set; }
}

readonly record struct ResolvedJointLimits(int HardMin, int HardMax, int SoftMin, int SoftMax);

public sealed class BodyCalibrationReport
{
    public BodyCalibrationReport(
        DateTimeOffset timestampUtc,
        string modelVersion,
        string rootJoint,
        bool strictMode,
        IReadOnlyList<BodyJointCalibrationSample> joints,
        IReadOnlyList<string> missingJoints)
    {
        TimestampUtc = timestampUtc;
        ModelVersion = modelVersion;
        RootJoint = rootJoint;
        StrictMode = strictMode;
        Joints = joints;
        MissingJoints = missingJoints;
    }

    public DateTimeOffset TimestampUtc { get; }
    public string ModelVersion { get; }
    public string RootJoint { get; }
    public bool StrictMode { get; }
    public IReadOnlyList<BodyJointCalibrationSample> Joints { get; }
    public IReadOnlyList<string> MissingJoints { get; }
}

public sealed class BodyJointCalibrationSample
{
    public BodyJointCalibrationSample(
        string jointName,
        string parentJoint,
        string axis,
        string location,
        ushort position,
        ushort load,
        ushort temperature,
        ushort voltage,
        int hardMin,
        int hardMax,
        int softMin,
        int softMax,
        bool withinHardRange,
        bool withinSoftRange,
        double softRangeProgress)
    {
        JointName = jointName;
        ParentJoint = parentJoint;
        Axis = axis;
        Location = location;
        Position = position;
        Load = load;
        Temperature = temperature;
        Voltage = voltage;
        HardMin = hardMin;
        HardMax = hardMax;
        SoftMin = softMin;
        SoftMax = softMax;
        WithinHardRange = withinHardRange;
        WithinSoftRange = withinSoftRange;
        SoftRangeProgress = softRangeProgress;
    }

    public string JointName { get; }
    public string ParentJoint { get; }
    public string Axis { get; }
    public string Location { get; }
    public ushort Position { get; }
    public ushort Load { get; }
    public ushort Temperature { get; }
    public ushort Voltage { get; }
    public int HardMin { get; }
    public int HardMax { get; }
    public int SoftMin { get; }
    public int SoftMax { get; }
    public bool WithinHardRange { get; }
    public bool WithinSoftRange { get; }
    public double SoftRangeProgress { get; }
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
        ushort temperature,
        ushort voltage,
        bool overload,
        bool thermalViolation,
        bool voltageViolation,
        bool communicationOk,
        string error,
        int overloadThreshold,
        int maxTemperature,
        int minVoltage)
    {
        MotorName = motorName;
        ID = id;
        Location = location;
        TorqueOn = torqueOn;
        Load = load;
        Temperature = temperature;
        Voltage = voltage;
        Overload = overload;
        ThermalViolation = thermalViolation;
        VoltageViolation = voltageViolation;
        CommunicationOk = communicationOk;
        Error = error;
        OverloadThreshold = overloadThreshold;
        MaxTemperature = maxTemperature;
        MinVoltage = minVoltage;
    }

    public string MotorName { get; }
    public byte ID { get; }
    public string Location { get; }
    public bool TorqueOn { get; }
    public ushort Load { get; }
    public ushort Temperature { get; }
    public ushort Voltage { get; }
    public bool Overload { get; }
    public bool ThermalViolation { get; }
    public bool VoltageViolation { get; }
    public bool CommunicationOk { get; }
    public string Error { get; }
    public int OverloadThreshold { get; }
    public int MaxTemperature { get; }
    public int MinVoltage { get; }
}
