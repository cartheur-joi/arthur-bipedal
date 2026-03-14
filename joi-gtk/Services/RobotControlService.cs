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

    public RobotControlService()
    {
        _motorControl = new MotorFunctions();
        _walkController = new WalkController(_motorControl);
    }

    public string Initialize()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureMaps();
        return _motorControl.InitializeDynamixelMotors().Trim();
    }

    public string TorqueOnLower()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureMaps();
        _motorControl.SetTorqueOn("lower");
        return "Lower-body torque enabled.";
    }

    public string TorqueOffLower()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureMaps();
        _motorControl.SetTorqueOff("lower");
        return "Lower-body torque disabled.";
    }

    public string ReadLowerTelemetry()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureMaps();
        Dictionary<string, int> snapshot = _motorControl.GetPresentPositions(Limbic.LeftLeg);
        foreach (KeyValuePair<string, int> kv in _motorControl.GetPresentPositions(Limbic.RightLeg))
            snapshot[kv.Key] = kv.Value;

        return "Lower pose: " + string.Join(", ", snapshot.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public string ExecuteWalkCycleSupervised(int cycles, int stepDurationMs, int interpolationSteps, int timeoutMs, bool requireSupportFootContact)
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureMaps();
        _walkController.RequireSupportFootContact = requireSupportFootContact;
        bool success = _walkController.ExecuteWalkCycleSupervised(cycles, stepDurationMs, interpolationSteps, timeoutMs);
        return success ? "Supervised walk completed." : "Supervised walk aborted by safety checks.";
    }

    public string EmergencyStopLower()
    {
        EnsureNativeDynamixelPrerequisites();
        EnsureMaps();
        _motorControl.SetTorqueOff("lower");
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
}
