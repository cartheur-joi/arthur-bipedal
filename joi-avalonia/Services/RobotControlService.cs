using Cartheur.Animals.Robot;
using System.Collections.Generic;
using System.Linq;

namespace joi_avalonia.Services;

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
        EnsureMaps();
        return _motorControl.InitializeDynamixelMotors().Trim();
    }

    public string TorqueOnLower()
    {
        EnsureMaps();
        _motorControl.SetTorqueOn("lower");
        return "Lower-body torque enabled.";
    }

    public string TorqueOffLower()
    {
        EnsureMaps();
        _motorControl.SetTorqueOff("lower");
        return "Lower-body torque disabled.";
    }

    public string ReadLowerTelemetry()
    {
        EnsureMaps();
        Dictionary<string, int> snapshot = _motorControl.GetPresentPositions(Limbic.LeftLeg);
        foreach (KeyValuePair<string, int> kv in _motorControl.GetPresentPositions(Limbic.RightLeg))
            snapshot[kv.Key] = kv.Value;

        return "Lower pose: " + string.Join(", ", snapshot.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public string ExecuteWalkCycleSupervised(int cycles, int stepDurationMs, int interpolationSteps, int timeoutMs, bool requireSupportFootContact)
    {
        EnsureMaps();
        _walkController.RequireSupportFootContact = requireSupportFootContact;
        bool success = _walkController.ExecuteWalkCycleSupervised(cycles, stepDurationMs, interpolationSteps, timeoutMs);
        return success ? "Supervised walk completed." : "Supervised walk aborted by safety checks.";
    }

    public string EmergencyStopLower()
    {
        EnsureMaps();
        _motorControl.SetTorqueOff("lower");
        return "Emergency stop applied: lower-body torque disabled.";
    }

    static void EnsureMaps()
    {
        if (Motor.MotorContext == null || Motor.MotorContext.Count == 0)
            MotorFunctions.CollateMotorArray();
    }
}
