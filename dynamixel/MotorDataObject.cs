namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// A static representation of a motor object.
    /// </summary>
    public class MotorDataObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MotorDataObject"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="presentPosition">The present position.</param>
        /// <param name="goalPosition">The goal position.</param>
        /// <param name="movingSpeed">The moving speed.</param>
        /// <param name="presentLoad">The present load.</param>
        /// <param name="torqueLimit">The torque limit.</param>
        /// <param name="lowerLimit">The lower limit.</param>
        /// <param name="upperLimit">The upper limit.</param>
        /// <param name="presentVoltage">The present voltage.</param>
        /// <param name="presentTemperature">The present temperature.</param>
        /// <param name="pid">The pid.</param>
        public MotorDataObject(string name, byte id, ushort presentPosition, ushort goalPosition, ushort movingSpeed, ushort presentLoad, ushort torqueLimit, ushort lowerLimit, ushort upperLimit, ushort presentVoltage, ushort presentTemperature)
        {
            Name = name;
            ID = id;
            PresentPosition = presentPosition;
            GoalPosition = goalPosition;
            MovingSpeed = movingSpeed;
            PresentLoad = presentLoad;
            TorqueLimit = torqueLimit;
            LowerLimit = lowerLimit;
            UpperLimit = upperLimit;
            PresentVoltage = presentVoltage;
            PresentTemperature = presentTemperature;
        }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public byte ID { get; set; }
        /// <summary>
        /// Gets or sets the present position.
        /// </summary>
        public ushort PresentPosition { get; set; }
        /// <summary>
        /// Gets or sets the goal position.
        /// </summary>
        public ushort GoalPosition { get; set; }
        /// <summary>
        /// Gets or sets the moving speed.
        /// </summary>
        public ushort MovingSpeed { get; set; }
        /// <summary>
        /// Gets or sets the present load. Control table value 126. Only for MX-28 in protocol 2.0.
        /// </summary>
        /// <remarks>https://emanual.robotis.com/docs/en/dxl/mx/mx-28-2/#present-load</remarks>
        public ushort PresentLoad { get; set; }
        /// <summary>
        /// Gets or sets the present current. Control table value 126. Only for MX-64 in protocol 2.0.
        /// </summary>
        /// <remarks>https://emanual.robotis.com/docs/en/dxl/mx/mx-64-2/#present-current126</remarks>
        public ushort PresentCurrent { get; set; }
        /// <summary>
        /// Gets or sets the current. Control table value 68. Only for MX-64 in protocol 1.0.
        /// </summary>
        /// <remarks>
        /// At an idle state without current flow, this value is 2,048(0x800). When positive current flows, this value becomes larger than 2,048(0x800) while negative current flow returns a value smaller than 2,048(0x800).
        /// 
        /// The following is current flow calculation formula.
        /// I = ( 4.5mA ) * (CURRENT – 2048 ) in amps unit(A). For example, 68 gives a value of 2148, which corresponds to 450mA of current flow.
        /// </remarks>
        public ushort Current { get; set; }
        /// <summary>
        /// Gets or sets the torque limit.
        /// </summary>
        public ushort TorqueLimit { get; set; }
        /// <summary>
        /// Gets or sets the lower limit.
        /// </summary>
        public ushort LowerLimit { get; set; }
        /// <summary>
        /// Gets or sets the upper limit.
        /// </summary>
        public ushort UpperLimit { get; set; }
        /// <summary>
        /// Gets or sets the present voltage.
        /// </summary>
        public ushort PresentVoltage { get; set; }
        /// <summary>
        /// Gets or sets the present temperature.
        /// </summary>
        public ushort PresentTemperature { get; set; }
        /// <summary>
        /// Gets or sets the pid.
        /// </summary>
        public ushort[] Pid { get; set; }
    }
}
