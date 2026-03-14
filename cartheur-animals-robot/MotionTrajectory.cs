using System;
using System.Collections.Generic;

namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// A timed target pose for a set of motors.
    /// </summary>
    public class MotionTrajectoryStep
    {
        public MotionTrajectoryStep(Dictionary<string, int> targets, int durationMilliseconds, int interpolationSteps = 8)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));
            if (targets.Count == 0)
                throw new ArgumentException("Trajectory step requires at least one motor target.", nameof(targets));

            Targets = new Dictionary<string, int>(targets);
            DurationMilliseconds = Math.Max(1, durationMilliseconds);
            InterpolationSteps = Math.Max(1, interpolationSteps);
        }

        public Dictionary<string, int> Targets { get; private set; }
        public int DurationMilliseconds { get; private set; }
        public int InterpolationSteps { get; private set; }
    }

    /// <summary>
    /// Executes timed trajectory steps using MotorFunctions.
    /// </summary>
    public class MotionTrajectoryPlayer
    {
        public MotionTrajectoryPlayer(MotorFunctions motorControl)
        {
            if (motorControl == null)
                throw new ArgumentNullException(nameof(motorControl));
            MotorControl = motorControl;
        }

        public MotorFunctions MotorControl { get; private set; }

        public void ExecuteStep(MotionTrajectoryStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            MotorControl.MoveMotorSequenceSmooth(step.Targets, step.DurationMilliseconds, step.InterpolationSteps);
        }

        public void ExecuteTrajectory(IEnumerable<MotionTrajectoryStep> steps)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));

            foreach (MotionTrajectoryStep step in steps)
            {
                ExecuteStep(step);
            }
        }
    }

    /// <summary>
    /// Builds a simple two-phase biped gait from a neutral pose.
    /// </summary>
    public static class BipedGaitFactory
    {
        public static IList<MotionTrajectoryStep> BuildTwoStepWalkCycle(
            Dictionary<string, int> neutralPose,
            int cycles = 1,
            int stepDurationMilliseconds = 450,
            int hipSwing = 50,
            int kneeBend = 35,
            int ankleCompensation = 20)
        {
            if (neutralPose == null)
                throw new ArgumentNullException(nameof(neutralPose));

            var steps = new List<MotionTrajectoryStep>();
            cycles = Math.Max(1, cycles);

            for (int i = 0; i < cycles; i++)
            {
                var leftSupport = Clone(neutralPose);
                ApplyOffset(leftSupport, "l_hip_y", -hipSwing);
                ApplyOffset(leftSupport, "r_hip_y", hipSwing);
                ApplyOffset(leftSupport, "r_knee_y", kneeBend);
                ApplyOffset(leftSupport, "r_ankle_y", -ankleCompensation);
                ApplyOffset(leftSupport, "abs_y", -ankleCompensation / 2);
                steps.Add(new MotionTrajectoryStep(leftSupport, stepDurationMilliseconds));

                var rightSupport = Clone(neutralPose);
                ApplyOffset(rightSupport, "r_hip_y", -hipSwing);
                ApplyOffset(rightSupport, "l_hip_y", hipSwing);
                ApplyOffset(rightSupport, "l_knee_y", kneeBend);
                ApplyOffset(rightSupport, "l_ankle_y", -ankleCompensation);
                ApplyOffset(rightSupport, "abs_y", ankleCompensation / 2);
                steps.Add(new MotionTrajectoryStep(rightSupport, stepDurationMilliseconds));
            }

            steps.Add(new MotionTrajectoryStep(Clone(neutralPose), stepDurationMilliseconds));
            return steps;
        }

        static Dictionary<string, int> Clone(Dictionary<string, int> source)
        {
            return new Dictionary<string, int>(source);
        }

        static void ApplyOffset(Dictionary<string, int> pose, string motor, int delta)
        {
            if (pose.ContainsKey(motor))
                pose[motor] += delta;
        }
    }
}
