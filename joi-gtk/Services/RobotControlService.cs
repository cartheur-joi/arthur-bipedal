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
        string result = _motorControl.InitializeDynamixelMotors().Trim();
        if (!MotorFunctions.DynamixelMotorsInitialized)
            throw new InvalidOperationException(result);

        int respondingLowerMotors = ScanLowerMotors();
        _initialized = true;
        return $"{result} Lower motor scan OK ({respondingLowerMotors} responding).";
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
