namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// The static class containing programmatic motor functions.
    /// </summary>
    public static class Motor
    {
        /// <summary>
        /// Gets or sets the name of the motor.
        /// </summary>
        public static string Name { get; set; }
        /// <summary>
        /// Gets or sets the identifier of the motor.
        /// </summary>
        public static int ID { get; set; }
        /// <summary>
        /// Gets or sets the position of the motor.
        /// </summary>
        public static double Position { get; set; }
        /// <summary>
        /// Dictionary of the motor context.
        /// </summary>
        public static Dictionary<string, byte> MotorContext { get; set; }
        /// <summary>
        /// Dictionary of the reverse motor context.
        /// </summary>
        public static Dictionary<byte, string> ReverseMotorContext { get; set; }
        /// <summary>
        /// Dictionary of the motor location.
        /// </summary>
        public static Dictionary<string, string> MotorLocation { get; set; }
        /// <summary>
        /// Dictionary of the port location.
        /// </summary>
        public static Dictionary<string, string> PortLocation { get; set; }
        public static Dictionary<string, byte> LeftLegMotorArray { get; set; }
        public static Dictionary<string, byte> RightLegMotorArray { get; set; }
        /// <summary>
        /// Returns the identifier of a named motor.
        /// </summary>
        /// <param name="name">The name of the motor.</param>
        /// <returns>The identifier of the named motor.</returns>
        public static byte ReturnID(string name)
        {
            byte value = 0;
            if (!MotorContext.ContainsKey(name))
            {
                return value;
            }
            if (MotorContext.ContainsKey(name))
            {
                MotorContext.TryGetValue(name, out value);
            }
            return value;
        }
        /// <summary>
        /// Returns the name of a motor.
        /// </summary>
        /// <param name="ID">The identifier of the moyor.</param>
        /// <returns></returns>
        public static string ReturnName(byte ID)
        {
            string value = "0";
            if (!ReverseMotorContext.ContainsKey(ID))
            {
                return value;
            }
            if (ReverseMotorContext.ContainsKey(ID))
            {
                ReverseMotorContext.TryGetValue(ID, out value);
            }
            return value;
        }
        /// <summary>
        /// Returns the location of the motor (upper or lower-half).
        /// </summary>
        /// <param name="name">The name of the motor.</param>
        /// <returns>The location either upper or lower.</returns>
        public static string ReturnLocation(string name)
        {
            string value = "0";
            if (!MotorLocation.ContainsKey(name))
            {
                return value;
            }
            if (MotorLocation.ContainsKey(name))
            {
                MotorLocation.TryGetValue(name, out value);
            }
            return value;
        }
        /// <summary>
        /// Returns the port location of the motor.
        /// </summary>
        /// <param name="name">The name of the motor.</param>
        /// <returns>The port location for port return register in the robotis motor.</returns>
        public static string ReturnPortLocation(string name)
        {
            string value = "0";
            if (!PortLocation.ContainsKey(name))
            {
                return value;
            }
            if (PortLocation.ContainsKey(name))
            {
                PortLocation.TryGetValue(name, out value);
            }
            return value;

        }
    }
}
