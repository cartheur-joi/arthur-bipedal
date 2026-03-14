namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// The collection of motors by limbic, or, analogous human limbs on the robot.
    /// </summary>
    /// <remarks>Pelvis is a part of the respective leg-limbic areas.</remarks>
    public static class Limbic
    {
        public enum LimbicArea { Abdomen, Bust, Head, LeftArm, RightArm, LeftPelvis, RightPelvis, LeftLeg, RightLeg }

        public static string[] Abdomen = { "abs_y", "abs_x", "abs_z" };
        public static string[] Bust = { "bust_y", "bust_x" };
        public static string[] Head = { "head_z", "head_y" };
        public static string[] LeftArm = { "l_shoulder_y", "l_shoulder_x", "l_arm_z", "l_elbow_y" };
        public static string[] RightArm = { "r_shoulder_y", "r_shoulder_x", "r_arm_z", "r_elbow_y" };
        public static string[] LeftLeg = { "l_hip_x", "l_hip_z", "l_hip_y", "l_knee_y", "l_ankle_y" };
        public static string[] RightLeg = { "r_hip_x", "r_hip_z", "r_hip_y", "r_knee_y", "r_ankle_y" };
        // Separated hip motors for the control keypad application.
        public static string[] LeftPelvis = { "l_hip_x" };
        public static string[] RightPelvis = { "r_hip_x" };
        public static string[] LeftLegNoPelvis = { "l_hip_z", "l_hip_y", "l_knee_y", "l_ankle_y" };
        public static string[] RightLegNoPelvis = { "r_hip_z", "r_hip_y", "r_knee_y", "r_ankle_y" };
        public static string[] LeftAnkle = { "l_ankle_y" };
        public static string[] RightAnkle = { "r_ankle_y" };
        /// <summary>
        /// The full list of limbic declarations for david.
        /// </summary>
        /// <remarks>A full-set of twenty-five motors.</remarks>
        public static string[] All = { "abs_y", "abs_x", "abs_z", "bust_y", "bust_x", "head_z", "head_y", "l_shoulder_y", "l_shoulder_x", "l_arm_z", "l_elbow_y", "r_shoulder_y", "r_shoulder_x", "r_arm_z", "r_elbow_y", "l_hip_z", "l_hip_y", "l_knee_y", "l_ankle_y", "l_hip_x", "r_hip_z", "r_hip_y", "r_knee_y", "r_ankle_y", "r_hip_x" };
    }
}
