using System.Runtime.InteropServices;

namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// The set of functions for desired motor behaviours.
    /// </summary>
    public class MotorFunctions
    {
        // Protocol version
        public const int ProtocolVersion = 1;
        // Baudrate
        public const int BAUDRATE = 1000000;
        // Variables for the baudrate.
        public static int BaudRate { get; set; }
        // Create a variable to note when something is done.
        public static bool CorrectlyExecuted { get; set; }
        public bool StartupInitialized { get; set; }
        public static bool BaudRateSet { get; set; }
        public static bool ActivePortSet = false;
        public static bool DynamixelMotorsInitialized = false;
        public const string DeviceUpper = "COM4";
        public const string DeviceLower = "COM5";
        // Return the methods from the PortHandler.
        public static int PortNumberUpper { get; set; }
        public static int PortNumberLower { get; set; }
        // Settings for the communcation paradigm.
        public const int ComSuccess = 0;
        public const int ComTxFail = -1001;
        public int ComResult = ComTxFail;
        // Create the variable for the error return.
        public byte Error = 0;
        // Motor primitive functions.
        // Set the torque on.
        public const int TorqueEnable = 1;
        // Set the torque off.
        public const int TorqueDisable = 0;
        // Register addresses for the present and goal position.
        public const int AddressAngleLimit = 8;// Restricting to CCW in the first iteration.
        public const int AddressGoalPosition = 30;
        public const int AddressMovingSpeed = 32;
        public const int AddressTorqueLimit = 34;
        public const int AddressPresentPosition = 36;
        public const int AddressPresentLoad = 40;
        public const int AddressPresentTemperature = 43;
        public const int AddressPresentVoltage = 42;
        // Control table address for Dynamixel MX-Series motors.
        public const int AxAddress = 24;
        public const int MxAddress = 24;
        // Limiting properties for the motors.
        public const int TorqueLimit = 500;// <-- Hardcoded to 20 in some calls.
        public const int MovingSpeed = 100;// Restricting to CCW in the first iteration.
        public const int PresentLoadAlarm = 900; // Absolute value.
        // Primitives for any animation.
        public static ushort PresentPosition { get; set; }
        /// <summary>
        /// Gets or sets the present load. And the present current?
        /// </summary>
        public ushort PresentLoad { get; set; }
        public ushort PresentTemperature { get; set; }
        public ushort PresentVoltage { get; set; }
        public ushort GoalPosition { get; set; }
        public ushort PresentMotorSpeed { get; set; }
        public ushort LowerLimit { get; set; }
        public ushort UpperLimit { get; set; }
        public ushort[] AngleLimit { get; set; }
        public ushort[] Pid { get; set; }
        public ushort TorqueOn { get; set; }
        public static Dictionary<string, int> RobotMotorSequenceAction { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="MotorFunctions"/> class.
        /// </summary>
        public MotorFunctions()
        {
            if (!ActivePortSet)
                SetActivePorts();
            // Add known basic parameters.
            Pid = new ushort[] { 4, 0, 0 };
        }
        public void SetActivePorts()
        {
            try
            {
                PortNumberUpper = Dynamixel.portHandler(DeviceUpper);
                PortNumberLower = Dynamixel.portHandler(DeviceLower);
                Dynamixel.packetHandler();
                ActivePortSet = true;
            }
            catch (Exception ex)
            {
                Logging.WriteLog(ex.Message, Logging.LogType.Error, Logging.LogCaller.MotorControl);
            }
        }
        public string InitializeDynamixelMotors()
        {
            if (MotorFunctions.ActivePortSet)
            {
                try
                {
                    Dynamixel.openPort(MotorFunctions.PortNumberUpper);
                    Dynamixel.openPort(MotorFunctions.PortNumberLower);
                    DynamixelMotorsInitialized = true;
                }
                catch (Exception ex)
                {
                    Logging.WriteLog(ex.Message, Logging.LogType.Error, Logging.LogCaller.MotorControl);
                    return ex.Message.ToString();
                }
            }
            return "Ports have been opened." + Environment.NewLine;
        }
        public string DisposeDynamixelMotors()
        {
            if (MotorFunctions.ActivePortSet)
            {
                try
                {
                    Dynamixel.closePort(MotorFunctions.PortNumberUpper);
                    Dynamixel.closePort(MotorFunctions.PortNumberLower);
                    DynamixelMotorsInitialized = false;
                }
                catch (Exception ex)
                {
                    Logging.WriteLog(ex.Message, Logging.LogType.Error, Logging.LogCaller.MotorControl);
                    return ex.Message.ToString();
                }
            }
            return "Ports have been closed." + Environment.NewLine;
        }
        /// <summary>
        /// The collection of robotic motor objects.
        /// </summary>
        public IList<MotorDataObject> RobotMotorObjects { get; set; }
        /// <summary>
        /// Collates the motor array from the dictionary objects.
        /// </summary>
        public static void CollateMotorArray()
        {
            Motor.MotorContext = new Dictionary<string, byte>
            {
                { "l_hip_x", 11 },
                { "l_hip_z", 12 },
                { "l_hip_y", 13 },
                { "l_knee_y", 14 },
                { "l_ankle_y", 15 },
                { "r_hip_x", 21 },
                { "r_hip_z", 22 },
                { "r_hip_y", 23 },
                { "r_knee_y", 24 },
                { "r_ankle_y", 25 },
                { "abs_y", 31 },
                { "abs_x", 32 },
                { "abs_z", 33 },
                { "bust_y", 34 },
                { "bust_x", 35 },
                { "head_z", 36 },
                { "head_y", 37 },
                { "l_shoulder_y", 41 },
                { "l_shoulder_x", 42 },
                { "l_arm_z", 43 },
                { "l_elbow_y", 44 },
                { "r_shoulder_y", 51 },
                { "r_shoulder_x", 52 },
                { "r_arm_z", 53 },
                { "r_elbow_y", 54 }
            };
            Motor.ReverseMotorContext = new Dictionary<byte, string>
            {
                { 11, "l_hip_x" },
                { 12, "l_hip_z" },
                { 13, "l_hip_y" },
                { 14, "l_knee_y" },
                { 15, "l_ankle_y" },
                { 21, "r_hip_x" },
                { 22, "r_hip_z" },
                { 23, "r_hip_y" },
                { 24, "r_knee_y" },
                { 25, "r_ankle_y" },
                { 31, "abs_y" },
                { 32, "abs_x" },
                { 33, "abs_z" },
                { 34, "bust_y" },
                { 35, "bust_x" },
                { 36, "head_z" },
                { 37, "head_y" },
                { 41, "l_shoulder_y" },
                { 42, "l_shoulder_x" },
                { 43, "l_arm_z" },
                { 44, "l_elbow_y" },
                { 51, "r_shoulder_y" },
                { 52, "r_shoulder_x" },
                { 53, "r_arm_z" },
                { 54, "r_elbow_y" }
            };
            Motor.MotorLocation = new Dictionary<string, string>
            {
                { "l_hip_x", "lower" },
                { "l_hip_z", "lower" },
                { "l_hip_y", "lower" },
                { "l_knee_y", "lower" },
                { "l_ankle_y", "lower" },
                { "r_hip_x", "lower" },
                { "r_hip_z", "lower" },
                { "r_hip_y", "lower" },
                { "r_knee_y", "lower" },
                { "r_ankle_y", "lower" },
                { "abs_y", "upper" },
                { "abs_x", "upper" },
                { "abs_z", "upper" },
                { "bust_y", "upper" },
                { "bust_x", "upper" },
                { "head_z", "upper" },
                { "head_y", "upper" },
                { "l_shoulder_y", "upper" },
                { "l_shoulder_x", "upper" },
                { "l_arm_z", "upper" },
                { "l_elbow_y", "upper" },
                { "r_shoulder_y", "upper" },
                { "r_shoulder_x", "upper" },
                { "r_arm_z", "upper" },
                { "r_elbow_y", "upper" }
            };
            Motor.PortLocation = new Dictionary<string, string>
            {
                { "lower", "PortReturnLower" },
                { "upper", "PortReturnUpper" }
            };
            Motor.LeftLegMotorArray = new Dictionary<string, byte>
            {
                { "l_hip_x", 11 },
                { "l_hip_z", 12 },
                { "l_hip_y", 13 },
                { "l_knee_y", 14 },
                { "l_ankle_y", 15 }
            };
            Motor.RightLegMotorArray = new Dictionary<string, byte>
            {
                { "r_hip_x", 21 },
                { "r_hip_z", 22 },
                { "r_hip_y", 23 },
                { "r_knee_y", 24 },
                { "r_ankle_y", 25 }
            };
        }
        /// <summary>
        /// Set the baudrate for the robot.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static bool SetBaudRate(string section)
        {
            if (section == "lower")
            {
                Dynamixel.setBaudRate(PortNumberLower, BAUDRATE);
                // Check that it was set correctly.
                BaudRate = Dynamixel.getBaudRate(PortNumberLower);
                if (BaudRate == BAUDRATE)
                    return true;
            }
            if (section == "upper")
            {
                if (CorrectlyExecuted = Dynamixel.setBaudRate(PortNumberUpper, BAUDRATE))
                {
                    BaudRate = Dynamixel.getBaudRate(PortNumberUpper);
                    if (BaudRate == BAUDRATE)
                        return true;
                }
            }
            if (section != "upper" && section != "lower")
            {
                var location = Motor.ReturnLocation(section);
                if (location == "upper")
                    CorrectlyExecuted = Dynamixel.setBaudRate(PortNumberUpper, BAUDRATE);
                if (location == "lower")
                    CorrectlyExecuted = Dynamixel.setBaudRate(PortNumberLower, BAUDRATE);
            }
            return false;
        }

        #region The gets for the motors
        public Dictionary<string, int> GetPresentPositions(string[] limbics)
        {
            var allPositions = new Dictionary<string, int>();
            for (int i = 0; i < limbics.Length; i++)
            {
                var location = Motor.ReturnLocation(limbics[i]);
                if (location == "upper")
                {
                    SetBaudRate(location);
                    PresentPosition = Dynamixel.read2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(limbics[i]), AddressPresentPosition);
                    allPositions.Add(limbics[i], PresentPosition);
                }
                else
                {
                    SetBaudRate(location);
                    PresentPosition = Dynamixel.read2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(limbics[i]), AddressPresentPosition);
                    allPositions.Add(limbics[i], PresentPosition);
                }
            }
            return allPositions;
        }
        public ushort GetPresentPosition(string motor)
        {
            var location = Motor.ReturnLocation(motor);
            if (location == "upper")
            {
                SetBaudRate(location);
                PresentPosition = Dynamixel.read2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(motor), AddressPresentPosition);
                return PresentPosition;
            }
            else
            {
                SetBaudRate(location);
                PresentPosition = Dynamixel.read2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(motor), AddressPresentPosition);
                return PresentPosition;
            }
        }
        public ushort GetPresentLoad(string motor)
        {
            var location = Motor.ReturnLocation(motor);
            if (location == "upper")
            {
                SetBaudRate(location);
                PresentLoad = Dynamixel.read2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(motor), AddressPresentLoad);
                return PresentLoad;
            }
            else
            {
                SetBaudRate("lower");
                PresentLoad = Dynamixel.read2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(motor), AddressPresentLoad);
                return PresentLoad;
            }
        }
        public ushort GetPresentTemperture(string motor)
        {
            var location = Motor.ReturnLocation(motor);
            if (location == "upper")
            {
                SetBaudRate(location);
                PresentTemperature = Dynamixel.read2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(motor), AddressPresentTemperature);
                return PresentTemperature;
            }
            else
            {
                SetBaudRate("lower");
                PresentTemperature = Dynamixel.read2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(motor), AddressPresentTemperature);
                return PresentTemperature;
            }
        }
        public ushort GetPresentVoltage(string motor)
        {
            var location = Motor.ReturnLocation(motor);
            if (location == "upper")
            {
                SetBaudRate(location);
                PresentVoltage = Dynamixel.read2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(motor), AddressPresentVoltage);
                return PresentVoltage;
            }
            else
            {
                SetBaudRate("lower");
                PresentVoltage = Dynamixel.read2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(motor), AddressPresentVoltage);
                return PresentVoltage;
            }
        }
        #endregion

        public void CreateConnectMotorObjects()
        {
            RobotMotorObjects = new List<MotorDataObject>();
            foreach (var motor in Motor.MotorContext)
            {
                RobotMotorObjects.Add(new MotorDataObject(motor.Key, motor.Value, GetPresentPosition(motor.Key), 0, 20, GetPresentLoad(motor.Key), TorqueLimit, LowerLimit, UpperLimit, GetPresentVoltage(motor.Key), GetPresentTemperture(motor.Key)));
            }
        }
        public void AddGoalPosition(string name, ushort goalPosition)
        {
            RobotMotorObjects.Where(c => c.Name == name).FirstOrDefault().GoalPosition = goalPosition;
        }
        public void AddLowerLimit(string name, ushort limit)
        {
            RobotMotorObjects.Where(c => c.Name == name).FirstOrDefault().LowerLimit = limit;
        }
        public void AddUpperLimit(string name, ushort limit)
        {
            RobotMotorObjects.Where(c => c.Name == name).FirstOrDefault().UpperLimit = limit;
        }
        public void IsTorqueOn(string region)
        {

        }
        public bool IsTorqueOn(string[] motors)
        {
            bool result = false;
            string motorArea = "";
            for (int i = 0; i < motors.Length; i++)
            {
                motorArea = Motor.ReturnLocation(motors[i]);
                if (motorArea == "upper")
                    TorqueOn = Dynamixel.read1ByteTxRx(PortNumberUpper, ProtocolVersion, Motor.ReturnID(motors[i]), MxAddress);
                if (motorArea == "lower")
                    TorqueOn = Dynamixel.read1ByteTxRx(PortNumberLower, ProtocolVersion, Motor.ReturnID(motors[i]), MxAddress);
                if (TorqueOn == 1)
                    return true;
                else
                    return result;
            }
            return result;
        }
        public void SetTorqueOn(string region)
        {
            switch (region)
            {
                case "all":
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX11, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX12, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX13, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX14, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX15, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX21, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX22, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX23, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX24, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX25, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX31, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX32, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX33, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX34, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX35, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.AXDX36, AxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.AXDX37, AxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX41, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX42, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX43, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX44, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX51, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX52, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX53, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX54, MxAddress, TorqueEnable);
                    break;
                case "lower":
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX11, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX12, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX13, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX14, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX15, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX21, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX22, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX23, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX24, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX25, MxAddress, TorqueEnable);
                    break;
                case "upper":
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX31, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX32, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX33, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX34, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX35, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.AXDX36, AxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.AXDX37, AxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX41, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX42, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX43, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX44, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX51, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX52, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX53, MxAddress, TorqueEnable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX54, MxAddress, TorqueEnable);
                    break;
                default:
                    return;
            }
        }
        public void SetTorqueOn(string[] motors)
        {
            string motorArea = "";
            for (int i = 0; i < motors.Length; i++)
            {
                motorArea = Motor.ReturnLocation(motors[i]);
                if (motorArea == "upper")
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Motor.ReturnID(motors[i]), MxAddress, TorqueEnable);
                if (motorArea == "lower")
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Motor.ReturnID(motors[i]), MxAddress, TorqueEnable);
            }
        }
        public void SetTorqueOff(string region)
        {
            switch (region)
            {
                case "all":
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX11, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX12, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX13, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX14, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX15, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX21, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX22, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX23, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX24, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX25, MxAddress, TorqueDisable);
                    // Disable torque for all the motors on the upper-half.
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX31, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX32, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX33, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX34, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX35, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.AXDX36, AxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.AXDX37, AxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX41, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX42, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX43, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX44, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX51, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX52, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX53, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX54, MxAddress, TorqueDisable);
                    break;
                case "lower":
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX11, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX12, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX13, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX14, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX15, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX21, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX22, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX23, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX24, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Identities.MXDX25, MxAddress, TorqueDisable);
                    break;
                case "upper":
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX31, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX32, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX33, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX34, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX35, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.AXDX36, AxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.AXDX37, AxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX41, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX42, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX43, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX44, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX51, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX52, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX53, MxAddress, TorqueDisable);
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Identities.MXDX54, MxAddress, TorqueDisable);
                    break;
                default:
                    return;
            }
        }
        public void SetTorqueOff(string[] motors)
        {
            string motorArea = "";
            for (int i = 0; i < motors.Length; i++) 
            {
                motorArea = Motor.ReturnLocation(motors[i]);
                if (motorArea == "upper")
                    Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, Motor.ReturnID(motors[i]), MxAddress, TorqueDisable);
                if (motorArea == "lower")
                    Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, Motor.ReturnID(motors[i]), MxAddress, TorqueDisable);
            }
        }
        public void MoveMotorSequence()
        {
            // Process the dictionary object list for the motors to set.
            foreach (KeyValuePair<string, int> element in RobotMotorSequenceAction)
            {
                var id = Motor.ReturnID(element.Key);
                UInt16[] goalPosition = new UInt16[2] { GetPresentPosition(element.Key), (ushort)element.Value };
                switch (Motor.ReturnLocation(element.Key))
                {
                    case "upper":
                        // Set the motor speed to something safer.
                        Dynamixel.write2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(element.Key), AddressMovingSpeed, (ushort)100);
                        // This address (goal position) takes a 2-byte command.
                        Dynamixel.write2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(element.Key), AddressGoalPosition, (ushort)element.Value);
                        if ((ComResult = Dynamixel.getLastTxRxResult(PortNumberUpper, 1)) != ComSuccess)
                        {
                            Logging.WriteLog("TxRx result: " + Marshal.PtrToStringAnsi(Dynamixel.getTxRxResult(1, ComResult)), Logging.LogType.Error, Logging.LogCaller.Marshal); 
                        }
                        else if ((Error = Dynamixel.getLastRxPacketError(PortNumberUpper, 1)) != 0)
                        {
                            Logging.WriteLog("Packet error: " + Marshal.PtrToStringAnsi(Dynamixel.getRxPacketError(1, Error)), Logging.LogType.Error, Logging.LogCaller.Marshal); 
                        }
                        break;
                    case "lower":
                        // Set the motor speed to something safer.
                        Dynamixel.write2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(element.Key), AddressMovingSpeed, (ushort)100);
                        Dynamixel.write2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(element.Key), AddressGoalPosition, (ushort)element.Value);
                        if ((ComResult = Dynamixel.getLastTxRxResult(PortNumberLower, 1)) != ComSuccess)
                        {
                            Logging.WriteLog("TxRx result: " + Marshal.PtrToStringAnsi(Dynamixel.getTxRxResult(1, ComResult)), Logging.LogType.Error, Logging.LogCaller.Marshal);
                        }
                        else if ((Error = Dynamixel.getLastRxPacketError(PortNumberLower, 1)) != 0)
                        {
                            Logging.WriteLog("Packet error: " + Marshal.PtrToStringAnsi(Dynamixel.getRxPacketError(1, Error)), Logging.LogType.Error, Logging.LogCaller.Marshal);
                        }
                        break;
                    default:
                        //return "0";
                        break;
                }
            }
        }
        /// <summary>
        /// Action a set of motor sequences based on those input from a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        public void MoveMotorSequence(Dictionary<string, int> dictionary)
        {
            // Process the dictionary object list for the motors to set.
            foreach (KeyValuePair<string, int> element in dictionary)
            {
                var id = Motor.ReturnID(element.Key);
                UInt16[] goalPosition = new UInt16[2] { GetPresentPosition(element.Key), (ushort)element.Value };
                switch (Motor.ReturnLocation(element.Key))
                {
                    case "upper":
                        // Set the motor speed to something safer.
                        Dynamixel.write2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(element.Key), AddressMovingSpeed, (ushort)100);
                        // This address (goal position) takes a 2-byte command.
                        Dynamixel.write2ByteTxRx(PortNumberUpper, 1, Motor.ReturnID(element.Key), AddressGoalPosition, (ushort)element.Value);
                        if ((ComResult = Dynamixel.getLastTxRxResult(PortNumberUpper, 1)) != ComSuccess)
                        {
                            Logging.WriteLog("TxRx result: " + Marshal.PtrToStringAnsi(Dynamixel.getTxRxResult(1, ComResult)), Logging.LogType.Error, Logging.LogCaller.Marshal);
                        }
                        else if ((Error = Dynamixel.getLastRxPacketError(PortNumberUpper, 1)) != 0)
                        {
                            Logging.WriteLog("Packet error: " + Marshal.PtrToStringAnsi(Dynamixel.getRxPacketError(1, Error)), Logging.LogType.Error, Logging.LogCaller.Marshal);
                        }
                        break;
                    case "lower":
                        // Set the motor speed to something safer.
                        Dynamixel.write2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(element.Key), AddressMovingSpeed, (ushort)100);
                        Dynamixel.write2ByteTxRx(PortNumberLower, 1, Motor.ReturnID(element.Key), AddressGoalPosition, (ushort)element.Value);
                        if ((ComResult = Dynamixel.getLastTxRxResult(PortNumberLower, 1)) != ComSuccess)
                        {
                            Logging.WriteLog("TxRx result: " + Marshal.PtrToStringAnsi(Dynamixel.getTxRxResult(1, ComResult)), Logging.LogType.Error, Logging.LogCaller.Marshal);
                        }
                        else if ((Error = Dynamixel.getLastRxPacketError(PortNumberLower, 1)) != 0)
                        {
                            Logging.WriteLog("Packet error: " + Marshal.PtrToStringAnsi(Dynamixel.getRxPacketError(1, Error)), Logging.LogType.Error, Logging.LogCaller.Marshal);
                        }
                        break;
                    default:
                        //return "0";
                        break;
                }
            }
        }

        #region Encapsulated animations

        public string RaiseArm()
        {
            // Begin by moving motor 41.
            var hereNow = GetPresentPosition(Motor.ReturnName((byte)41));
            ushort moveto = (ushort)(hereNow + 100);
            UInt16[] goalPosition = new UInt16[2] { GetPresentPosition(Motor.ReturnName((byte)41)), moveto };

            Dynamixel.write2ByteTxRx(PortNumberUpper, 1, 41, AddressGoalPosition, moveto);

            if ((ComResult = Dynamixel.getLastTxRxResult(PortNumberUpper, 1)) != ComSuccess)
            {
                return Marshal.PtrToStringAnsi(Dynamixel.getTxRxResult(1, ComResult));
            }
            else if ((Error = Dynamixel.getLastRxPacketError(PortNumberUpper, 1)) != 0)
            {
                return Marshal.PtrToStringAnsi(Dynamixel.getRxPacketError(1, Error));
            }

            return GetPresentPosition(Motor.ReturnName((byte)41)) + Environment.NewLine;
        }

        #endregion
    }
}
