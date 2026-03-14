//
// This autonomous intelligent system software is the property of Cartheur Research B.V. Copryright 2025, all rights reserved.
//
namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// Commands recognized by joi to perform an activity outside of its conversational significance.
    /// </summary>
    /// <remarks>These should NOT be hardcoded but pulled-in from a file.</remarks>
    public class Syntax
    {
        #region Lock motors for pose-training



        #endregion

        #region Commands for different animation vectors
        /// <summary>
        /// The left-arm animation command.
        /// </summary>
        public static string RaiseLeftArmAnimation = "extend your left arm in front of you";
        /// <summary>
        /// Reverting the left-arm animation command.
        /// </summary>
        public static string RevertLeftArmAnimation = "revert your left arm";
        /// <summary>
        /// The left-leg animation command.
        /// </summary>
        public static string RaiseLeftLegAnimation = "raise your left leg";
        /// <summary>
        /// Reverting the left-leg animation command.
        /// </summary>
        public static string RevertLeftLegAnimation = "lower your left leg";
        /// <summary>
        /// The right-arm animation command.
        /// </summary>
        public static string RaiseRightArmAnimation = "extend your right arm in front of you";
        /// <summary>
        /// Reverting the right-arm animation command.
        /// </summary>
        public static string RevertRightArmAnimation = "revert your right arm";
        /// <summary>
        /// The right-arm animation command.
        /// </summary>
        public static string RaiseRightLegAnimation = "raise your right leg";
        /// <summary>
        /// Reverting the right-leg animation command.
        /// </summary>
        public static string RevertRightLegAnimation = "lower your right leg";
        #endregion

        #region General commands
        /// <summary>
        /// The aeon listen command, inclusive trailing space.
        /// </summary>
        public static string RobotListenCommand = "joi ";
        /// <summary>
        /// The listen command using more obvious intonation from the speaker and that cannot be confused with ordinary words.
        /// </summary>
        public static string ListenCommand = "joitow";// Not used but keeping it here.
        /// <summary>
        /// The complex rest position command.
        /// </summary>
        public static string RestPositionCommand = "Take the rest position";
        /// <summary>
        /// The complex rest position command.
        /// </summary>
        public static string StandUpCommand = "Stand up";
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether [command received].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [command received]; otherwise, <c>false</c>.
        /// </value>
        public static bool CommandReceived { get; set; }
    }
}
