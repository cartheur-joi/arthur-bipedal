using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// High-level controller for executing walking cycles with basic runtime safety checks.
    /// </summary>
    public class WalkController
    {
        public WalkController(MotorFunctions motorControl = null, IImuProvider imuProvider = null, IFootContactProvider footContactProvider = null)
        {
            MotorControl = motorControl ?? new MotorFunctions();
            TrajectoryPlayer = new MotionTrajectoryPlayer(MotorControl);
            ImuProvider = imuProvider ?? new NullImuProvider();
            FootContactProvider = footContactProvider ?? new NullFootContactProvider();
            MaxLoad = MotorFunctions.PresentLoadAlarm;
            MaxTemperature = 70;
            MinVoltage = 90;
            MaxAbsPitchDegrees = 10.0;
            MaxAbsRollDegrees = 10.0;
            RequireSupportFootContact = true;
        }

        public MotorFunctions MotorControl { get; private set; }
        public MotionTrajectoryPlayer TrajectoryPlayer { get; private set; }
        public IImuProvider ImuProvider { get; private set; }
        public IFootContactProvider FootContactProvider { get; private set; }
        public int MaxLoad { get; set; }
        public int MaxTemperature { get; set; }
        public int MinVoltage { get; set; }
        public double MaxAbsPitchDegrees { get; set; }
        public double MaxAbsRollDegrees { get; set; }
        public bool RequireSupportFootContact { get; set; }

        /// <summary>
        /// Executes a walking sequence based on the current robot pose.
        /// </summary>
        /// <returns>True when all steps execute successfully.</returns>
        public bool ExecuteWalkCycle(int cycles = 1, int stepDurationMilliseconds = 450, int interpolationSteps = 8)
        {
            int timeoutMilliseconds = Math.Max(5000, (cycles * 3 + 1) * stepDurationMilliseconds);
            return ExecuteWalkCycleSupervised(cycles, stepDurationMilliseconds, interpolationSteps, timeoutMilliseconds);
        }

        /// <summary>
        /// Executes a walking cycle with timeout and sensor-driven stop conditions.
        /// </summary>
        public bool ExecuteWalkCycleSupervised(int cycles, int stepDurationMilliseconds, int interpolationSteps, int timeoutMilliseconds)
        {
            EnsureMotorMap();
            if (!EnsurePortsReady())
                return false;

            string[] involvedMotors = Limbic.All;
            EnsureTorqueOn(involvedMotors);

            var neutralPose = MotorControl.GetPresentPositions(involvedMotors);
            var softLimits = BuildSoftLimits(neutralPose);
            var steps = BipedGaitFactory.BuildTwoStepWalkCycle(neutralPose, cycles, stepDurationMilliseconds);
            Stopwatch stopwatch = Stopwatch.StartNew();
            int stepIndex = 0;

            foreach (var step in steps)
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMilliseconds)
                {
                    Logging.WriteLog("Walk cycle aborted because the supervised timeout was reached.", Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }

                SupportFoot expectedSupportFoot = InferExpectedSupportFoot(neutralPose, step.Targets, stepIndex, steps.Count);
                if (!ValidateTelemetry(step.Targets.Keys, expectedSupportFoot))
                {
                    Logging.WriteLog("Walk cycle aborted because telemetry exceeded safe thresholds.", Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }

                var clampedTargets = ClampPose(step.Targets, softLimits);
                var safeStep = new MotionTrajectoryStep(clampedTargets, step.DurationMilliseconds, interpolationSteps);
                TrajectoryPlayer.ExecuteStep(safeStep);
                stepIndex++;
            }

            return true;
        }

        /// <summary>
        /// Captures current positions as the neutral pose for gait generation.
        /// </summary>
        public Dictionary<string, int> CaptureNeutralPose(string[] motors = null)
        {
            EnsureMotorMap();
            string[] captureMotors = motors ?? Limbic.All;
            return MotorControl.GetPresentPositions(captureMotors);
        }

        /// <summary>
        /// Builds soft limits centered around a neutral pose.
        /// </summary>
        public Dictionary<string, Tuple<int, int>> BuildSoftLimits(Dictionary<string, int> neutralPose, int halfRange = 180)
        {
            if (neutralPose == null)
                throw new ArgumentNullException(nameof(neutralPose));

            var limits = new Dictionary<string, Tuple<int, int>>();
            int boundedHalfRange = Math.Max(1, halfRange);
            foreach (var kv in neutralPose)
            {
                int min = Math.Max(0, kv.Value - boundedHalfRange);
                int max = Math.Min(1023, kv.Value + boundedHalfRange);
                limits[kv.Key] = Tuple.Create(min, max);
            }
            return limits;
        }

        /// <summary>
        /// Clamps pose targets to configured soft limits.
        /// </summary>
        public Dictionary<string, int> ClampPose(Dictionary<string, int> pose, Dictionary<string, Tuple<int, int>> limits)
        {
            if (pose == null)
                throw new ArgumentNullException(nameof(pose));
            if (limits == null)
                throw new ArgumentNullException(nameof(limits));

            var result = new Dictionary<string, int>();
            foreach (var kv in pose)
            {
                if (limits.ContainsKey(kv.Key))
                {
                    int min = limits[kv.Key].Item1;
                    int max = limits[kv.Key].Item2;
                    result[kv.Key] = Math.Min(max, Math.Max(min, kv.Value));
                }
                else
                {
                    result[kv.Key] = kv.Value;
                }
            }
            return result;
        }

        void EnsureMotorMap()
        {
            if (Motor.MotorContext == null || Motor.MotorContext.Count == 0)
                MotorFunctions.CollateMotorArray();
        }

        bool EnsurePortsReady()
        {
            if (MotorFunctions.DynamixelMotorsInitialized)
                return true;

            string result = MotorControl.InitializeDynamixelMotors();
            if (result.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
            {
                Logging.WriteLog(result, Logging.LogType.Error, Logging.LogCaller.MotorControl);
                return false;
            }
            return true;
        }

        void EnsureTorqueOn(IEnumerable<string> motors)
        {
            string[] motorArray = motors.ToArray();
            if (!MotorControl.IsTorqueOn(motorArray))
                MotorControl.SetTorqueOn(motorArray);
        }

        bool ValidateTelemetry(IEnumerable<string> motors, SupportFoot expectedSupportFoot)
        {
            ImuSample imuSample = ImuProvider.GetSample();
            if (imuSample.IsValid)
            {
                if (Math.Abs(imuSample.PitchDegrees) > MaxAbsPitchDegrees)
                {
                    Logging.WriteLog("Unsafe pitch detected: " + imuSample.PitchDegrees, Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
                if (Math.Abs(imuSample.RollDegrees) > MaxAbsRollDegrees)
                {
                    Logging.WriteLog("Unsafe roll detected: " + imuSample.RollDegrees, Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
            }

            if (RequireSupportFootContact)
            {
                FootContactSample contactSample = FootContactProvider.GetSample();
                if (contactSample.IsValid && !IsExpectedFootInContact(contactSample, expectedSupportFoot))
                {
                    Logging.WriteLog("Support foot contact is inconsistent with expected stance phase.", Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
            }

            foreach (string motor in motors)
            {
                int load = MotorControl.GetPresentLoad(motor);
                int temperature = MotorControl.GetPresentTemperture(motor);
                int voltage = MotorControl.GetPresentVoltage(motor);

                if (Math.Abs(load) > MaxLoad)
                {
                    Logging.WriteLog("Unsafe load on " + motor + ": " + load, Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
                if (temperature > MaxTemperature)
                {
                    Logging.WriteLog("Unsafe temperature on " + motor + ": " + temperature, Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
                if (voltage < MinVoltage)
                {
                    Logging.WriteLog("Unsafe voltage on " + motor + ": " + voltage, Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
            }

            return true;
        }

        static SupportFoot InferExpectedSupportFoot(Dictionary<string, int> neutralPose, Dictionary<string, int> targetPose, int stepIndex, int totalStepCount)
        {
            // Last step returns to neutral.
            if (stepIndex >= totalStepCount - 1)
                return SupportFoot.Both;

            int leftNeutral = neutralPose.ContainsKey("l_knee_y") ? neutralPose["l_knee_y"] : 0;
            int rightNeutral = neutralPose.ContainsKey("r_knee_y") ? neutralPose["r_knee_y"] : 0;
            int leftTarget = targetPose.ContainsKey("l_knee_y") ? targetPose["l_knee_y"] : leftNeutral;
            int rightTarget = targetPose.ContainsKey("r_knee_y") ? targetPose["r_knee_y"] : rightNeutral;

            int leftDelta = leftTarget - leftNeutral;
            int rightDelta = rightTarget - rightNeutral;

            if (leftDelta > rightDelta)
                return SupportFoot.Right;
            if (rightDelta > leftDelta)
                return SupportFoot.Left;

            return SupportFoot.Both;
        }

        static bool IsExpectedFootInContact(FootContactSample sample, SupportFoot expectedSupportFoot)
        {
            switch (expectedSupportFoot)
            {
                case SupportFoot.Left:
                    return sample.LeftInContact;
                case SupportFoot.Right:
                    return sample.RightInContact;
                case SupportFoot.Both:
                    return sample.LeftInContact && sample.RightInContact;
                default:
                    return true;
            }
        }
    }
}
