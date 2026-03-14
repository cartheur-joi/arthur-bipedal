using Cartheur.Animals.Robot;
using DynamixelWizard.Controls;
using System.IO.Ports;
using System.Timers;

namespace DynamixelWizard.SubForms
{
    /// <summary>
    /// For direct control of the printed robot.
    /// </summary>
    public partial class TemplaterForm : Form
    {
        const int StepDelay = 3000;
        public static bool Instance { get; set; }
        public static SerialPort SerialPort { get; set; }
        public Thread SerialPortThread { get; set; }
        public static Dictionary<string, int> CurrentPositionsLeftArm { get; set; }
        public static Dictionary<string, int> DesiredPositionsLeftArm { get; set; }
        public static Dictionary<string, int> CurrentPositionsRightArm { get; set; }
        public static Dictionary<string, int> DesiredPositionsRightArm { get; set; }
        public static Dictionary<string, int> CurrentPositionsLeftLeg { get; set; }
        public static Dictionary<string, int> DesiredPositionsLeftLeg { get; set; }
        public static Dictionary<string, int> CurrentPositionsRightLeg { get; set; }
        public static Dictionary<string, int> DesiredPositionsRightLeg { get; set; }
        public MotorFunctions MotorControl { get; set; }
        public MotorSequence MotorSequences { get; set; }
        public bool MotorsInitialized { get; set; }
        public Remember RememberThings { get; set; }
        public Remember TrainingSequence { get; set; }
        public Remember StoredPositions { get; set; }
        public System.Windows.Forms.Timer TrainingStepTimer { get; set; }
        public System.Windows.Forms.Timer CountdownTimer { get; set; }
        public int TrainingStepTimeRemaining { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public int NumberOfTrainingSteps { get; set; }
        public int TrainingStepNumber { get; set; }

        // The collection of motor properties in one object.
        //public MotorData<string, int, ushort, ushort, ushort> MotorDataObject { get; set; }
        public IList<MotorDataObject> RobotMotorObjects { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplaterForm"/> class.
        /// </summary>
        public TemplaterForm()
        {
            InitializeComponent();
            areaMotorSelection.SelectedItem = "all";
            MotorControl = new MotorFunctions();
            MotorSequences = new MotorSequence();
            MotorsInitialized = true;
            RememberThings = new Remember();
            TrainingSequence = new Remember(@"\db\trainings.db");
            StoredPositions = new Remember(@"\db\positions.db");
            limbComboBox.SelectedItem = "arm";
            sideComboBox.SelectedItem = "left";
        }
        void InvokeControl(string? stream, string message)
        {
            if (logWindow.InvokeRequired)
            {
                logWindow.Invoke(new MethodInvoker(delegate { Name = logWindow.Text; }));
                this.Invoke(new MethodInvoker(delegate
                {
                    logWindow.Text = message + Environment.NewLine;
                    logWindow.ScrollToCaret();
                    ParseCoordinates(stream);
                }));
            }
        }
        void TriggerTorqueIndicator(bool engage, string message)
        {
            if (engage)
            {
                toolStripStatusLabel.Text = message;
                torqueStatusIndicator.FlasherButtonColorOn = Color.DarkGreen;
                torqueStatusIndicator.FlasherButtonStart(FlashIntervalSpeed.BlipFast);
                torqueStatusIndicator.FlashNumber = 10;
            }
            if (!engage)
            {
                toolStripStatusLabel.Text = message;
                torqueStatusIndicator.FlasherButtonColorOn = Color.DeepPink;
                torqueStatusIndicator.FlasherButtonStart(FlashIntervalSpeed.BlipFast);
            }
        }
        void PollSerialPort()
        {
            SerialPort = new SerialPort
            {
                PortName = "COM3",
                BaudRate = 115200
            };
            try
            {
                SerialPort.Open();
                while (true)
                {
                    string a = SerialPort.ReadExisting();
                    InvokeControl(a, "Serial port connected.");
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                InvokeControl("", ex.Message);
            }
        }
        void ParseCoordinates(string input)
        {
            if (input != "")
            {
                var coords = input.Split(new char[] { ';' });
                for (var i = 0; i < coords.Length; i++)
                {
                    var sum = coords[i].Split('=');
                    switch (sum[0].Trim())
                    {
                        case "Xc":
                            xcPositionLabel.Text = sum[1]; break;
                        case "Yc":
                            ycPositionLabel.Text = sum[1]; break;
                        case "Zc":
                            zcPositionLabel.Text = sum[1]; break;
                        case "Xs":
                            xsPositionLabel.Text = sum[1]; break;
                        case "Ys":
                            ysPositionLabel.Text = sum[1]; break;
                        case "Zs":
                            zsPositionLabel.Text = sum[1]; break;
                        case "Xw":
                            xwPositionLabel.Text = sum[1]; break;
                        case "Yw":
                            ywPositionLabel.Text = sum[1]; break;
                        case "Zw":
                            zwPositionLabel.Text = sum[1]; break;
                    }
                }
            }
        }

        #region Recorded training session managemeent

        public void RevertLimb()
        {
            switch ("")
            {
                case "Left arm":
                    MotorSequences.ReplayLimbicPosition(Limbic.LeftArm, StoredPositions);
                    break;
                case "Left leg":
                    MotorSequences.ReplayLimbicPosition(Limbic.LeftLeg, StoredPositions);
                    break;
                case "Right arm":
                    MotorSequences.ReplayLimbicPosition(Limbic.RightArm, StoredPositions);
                    break;
                case "Right leg":
                    MotorSequences.ReplayLimbicPosition(Limbic.RightLeg, StoredPositions);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Events
        private void MovementEvent(object source, ElapsedEventArgs e)
        {
            // Write the value of goal position to present position, else repoll it.
        }
        private void TorqueOnButtonClick(object sender, EventArgs e)
        {
            if (MotorsInitialized)
            {
                if (areaMotorSelection.SelectedItem == "lower")
                {
                    try
                    {
                        MotorControl.SetTorqueOn("lower");
                    }
                    catch (Exception ex)
                    {
                        logWindow.Text += "Problem with operation: " + ex.Message + Environment.NewLine;
                    }
                    finally
                    {
                        logWindow.Text += "Torque applied correctly for the lower-half." + Environment.NewLine;
                    }
                }
                if (areaMotorSelection.SelectedItem == "upper")
                {
                    try
                    {
                        MotorControl.SetTorqueOn("upper");
                    }
                    catch (Exception ex)
                    {
                        logWindow.Text += "Problem with operation: " + ex.Message + Environment.NewLine;
                    }
                    finally
                    {
                        logWindow.Text += "Torque applied correctly for the upper-half." + Environment.NewLine;
                    }
                }
                if (areaMotorSelection.SelectedItem == "all")
                {
                    try
                    {
                        MotorControl.SetTorqueOn("all");
                    }
                    catch (Exception ex)
                    {
                        logWindow.Text += "Problem with operation: " + ex.Message + Environment.NewLine;
                    }
                    finally
                    {
                        logWindow.Text += "Torque applied correctly for the entire robot." + Environment.NewLine;
                    }
                }
            }
        }
        private void TorqueOffButtonClick(object sender, EventArgs e)
        {
            if (MotorsInitialized)
            {
                if (areaMotorSelection.SelectedItem == "lower")
                {
                    try
                    {
                        MotorControl.SetTorqueOff("lower");
                    }
                    catch (Exception ex)
                    {
                        logWindow.Text += "Problem with operation: " + ex.Message + Environment.NewLine;
                    }
                    logWindow.Text += "Torque removed correctly for the lower-half." + Environment.NewLine;
                }
                if (areaMotorSelection.SelectedItem == "upper")
                {
                    try
                    {
                        MotorControl.SetTorqueOff("upper");
                    }
                    catch (Exception ex)
                    {
                        logWindow.Text += "Problem with operation: " + ex.Message + Environment.NewLine;
                    }
                    finally
                    {
                        logWindow.Text += "Torque removed correctly for the upper-half." + Environment.NewLine;
                    }
                }
                if (areaMotorSelection.SelectedItem == "all")
                {
                    try
                    {
                        MotorControl.SetTorqueOff("all");
                    }
                    catch (Exception ex)
                    {
                        logWindow.Text += "Problem with operation: " + ex.Message + Environment.NewLine;
                    }
                    finally
                    {
                        logWindow.Text += "Torque removed correctly for the entire robot." + Environment.NewLine;
                    }
                }
            }
        }
        private void GranularTorqueOnButtonClick(object sender, EventArgs e)
        {
            //if (motorSelectionList.SelectedItem == null)
            //{
            //    logWindow.Text += "Select a motor to set the torque for." + Environment.NewLine;
            //    return;
            //}
            //if (PortInitialized)
            //{
            //    SetBaudRate(motorSelectionList.SelectedItem.ToString());
            //    var motorId = Motor.ReturnID(motorSelectionList.SelectedItem.ToString());
            //    var motorArea = Motor.ReturnLocation(motorSelectionList.SelectedItem.ToString());
            //    if (motorArea == "upper")
            //        Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, motorId, MxAddress, TorqueEnable);
            //    if (motorArea == "lower")
            //        Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, motorId, MxAddress, TorqueEnable);
            //}
            //else
            //{
            //    logWindow.Text += "The port is not open. Turn on the robot and its connectivity hardware." + Environment.NewLine;
            //}
        }
        private void GranularTorqueOffButtonClick(object sender, EventArgs e)
        {
            //if (motorSelectionList.SelectedItem == null)
            //{
            //    logWindow.Text += "Select a motor to set the torque for." + Environment.NewLine;
            //    return;
            //}
            //if (PortInitialized)
            //{
            //    SetBaudRate(motorSelectionList.SelectedItem.ToString());
            //    var motorId = Motor.ReturnID(motorSelectionList.SelectedItem.ToString());
            //    var motorArea = Motor.ReturnLocation(motorSelectionList.SelectedItem.ToString());
            //    if (motorArea == "upper")
            //        Dynamixel.write1ByteTxRx(PortNumberUpper, ProtocolVersion, motorId, MxAddress, TorqueDisable);
            //    if (motorArea == "lower")
            //        Dynamixel.write1ByteTxRx(PortNumberLower, ProtocolVersion, motorId, MxAddress, TorqueDisable);
            //}
            //else
            //{
            //    logWindow.Text += "The port is not open. Turn on the robot and its connectivity hardware." + Environment.NewLine;
            //}
        }
        private void CloseButtonClick(object sender, EventArgs e)
        {
            // Clean-up resources and close.
            if (SerialPortThread != null)
            {
                if (SerialPortThread.IsAlive)
                {
                    // SerialPortThread.Suspend();
                    SerialPortThread = null;
                    SerialPort.Close();
                    SerialPort.Dispose();
                }
            }
            Instance = false;
            Close();
        }
        private void GetPositionButtonClick(object sender, EventArgs e)
        {
            logWindow.Text += MotorControl.GetPresentPosition(motorSelectionList.SelectedItem.ToString()) + Environment.NewLine;
        }
        private void RaiseArmClick(object sender, EventArgs e)
        {
            // Replay.
            if (limbComboBox.SelectedItem.ToString() == "arm" & sideComboBox.SelectedItem.ToString() == "left")
            {
                // 1. Set to the first known set of positions of the arm. Torque has already been set.
                MotorControl.MoveMotorSequence(CurrentPositionsLeftArm);
                Thread.Sleep(StepDelay);
                // 2. Move to the new desired set of positions of the arm.
                MotorControl.MoveMotorSequence(DesiredPositionsLeftArm);
                Thread.Sleep(StepDelay);
                // 3. Return to the first known set of positions of the arm. Set torque off.
                MotorControl.MoveMotorSequence(CurrentPositionsLeftArm);
                Thread.Sleep(StepDelay);
                MotorControl.SetTorqueOff(Limbic.LeftArm);
                MotorControl.SetTorqueOff(Limbic.Abdomen);
                MotorControl.SetTorqueOff(Limbic.RightArm);
                MotorControl.SetTorqueOff(Limbic.Head);
                MotorControl.SetTorqueOff(Limbic.Bust);
                TriggerTorqueIndicator(false, "Torque released.");
            }
            if (limbComboBox.SelectedItem.ToString() == "arm" & sideComboBox.SelectedItem.ToString() == "right")
            {
                // 1. Set to the first known set of positions of the arm. Torque has already been set.
                MotorControl.MoveMotorSequence(CurrentPositionsRightArm);
                Thread.Sleep(StepDelay);
                // 2. Move to the new desired set of positions of the arm.
                MotorControl.MoveMotorSequence(DesiredPositionsRightArm);
                Thread.Sleep(StepDelay);
                // 3. Return to the first known set of positions of the arm. Set torque off.
                MotorControl.MoveMotorSequence(CurrentPositionsRightArm);
                Thread.Sleep(StepDelay);
                MotorControl.SetTorqueOff(Limbic.LeftArm);
                MotorControl.SetTorqueOff(Limbic.Abdomen);
                MotorControl.SetTorqueOff(Limbic.RightArm);
                MotorControl.SetTorqueOff(Limbic.Head);
                MotorControl.SetTorqueOff(Limbic.Bust);
                TriggerTorqueIndicator(false, "Torque released.");
            }
        }
        private void PositionNowFlashButtonClick(object sender, EventArgs e)
        {
            if (MotorsInitialized)
            {
                if (limbComboBox.SelectedItem.ToString() == "arm")
                {
                    if (sideComboBox.SelectedItem.ToString() == "left")
                    {
                        CurrentPositionsLeftArm = MotorSequences.ReturnDictionaryOfPositions(Limbic.LeftArm);
                        toolStripStatusLabel.Text = "Motors added to current positions dictionary (left arm).";
                        // The line above, thanks to 2007s RSSReader.
                        MotorControl.SetTorqueOn(Limbic.Abdomen);
                        MotorControl.SetTorqueOn(Limbic.RightArm);
                        MotorControl.SetTorqueOn(Limbic.Head);
                        MotorControl.SetTorqueOn(Limbic.Bust);
                        TriggerTorqueIndicator(true, "Torque engaged.");
                    }
                    if (sideComboBox.SelectedItem.ToString() == "right")
                    {
                        CurrentPositionsRightArm = MotorSequences.ReturnDictionaryOfPositions(Limbic.RightArm);
                        toolStripStatusLabel.Text = "Motors added to current positions dictionary (right arm).";
                        MotorControl.SetTorqueOn(Limbic.Abdomen);
                        MotorControl.SetTorqueOn(Limbic.LeftArm);
                        MotorControl.SetTorqueOn(Limbic.Head);
                        MotorControl.SetTorqueOn(Limbic.Bust);
                        TriggerTorqueIndicator(true, "Torque engaged.");
                    }
                    positionNowFlashButton.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
                    positionNowFlashButton.FlashNumber = 2;
                }
                if (limbComboBox.SelectedItem.ToString() == "leg")
                {
                    if (sideComboBox.SelectedItem.ToString() == "left")
                    {
                        CurrentPositionsLeftLeg = MotorSequences.ReturnDictionaryOfPositions(Limbic.LeftLeg);
                        toolStripStatusLabel.Text = "Motors added to current positions dictionary (left leg).";
                        // The line above, thanks to 2007s RSSReader.
                        MotorControl.SetTorqueOn(Limbic.Abdomen);
                        MotorControl.SetTorqueOn(Limbic.LeftArm);
                        MotorControl.SetTorqueOn(Limbic.RightArm);
                        MotorControl.SetTorqueOn(Limbic.Head);
                        MotorControl.SetTorqueOn(Limbic.Bust);
                        TriggerTorqueIndicator(true, "Torque engaged.");
                    }
                    if (sideComboBox.SelectedItem.ToString() == "right")
                    {
                        CurrentPositionsRightLeg = MotorSequences.ReturnDictionaryOfPositions(Limbic.RightLeg);
                        toolStripStatusLabel.Text = "Motors added to current positions dictionary (right arm).";
                        MotorControl.SetTorqueOn(Limbic.Abdomen);
                        MotorControl.SetTorqueOn(Limbic.LeftArm);
                        MotorControl.SetTorqueOn(Limbic.RightArm);
                        MotorControl.SetTorqueOn(Limbic.Head);
                        MotorControl.SetTorqueOn(Limbic.Bust);
                        TriggerTorqueIndicator(true, "Torque engaged.");
                    }
                    positionNowFlashButton.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
                    positionNowFlashButton.FlashNumber = 2;
                }
            }
        }
        private void DesiredPositionButtonClick(object sender, EventArgs e)
        {
            if (MotorsInitialized)
            {
                if (limbComboBox.SelectedItem.ToString() == "arm")
                {
                    if (sideComboBox.SelectedItem.ToString() == "left")
                    {
                        DesiredPositionsLeftArm = MotorSequences.ReturnDictionaryOfPositions(Limbic.LeftArm);
                        toolStripStatusLabel.Text = "Motors added to desired positions dictionary (left arm).";
                    }
                    if (sideComboBox.SelectedItem.ToString() == "right")
                    {
                        DesiredPositionsRightArm = MotorSequences.ReturnDictionaryOfPositions(Limbic.RightArm);
                        toolStripStatusLabel.Text = "Motors added to desired positions dictionary (right arm).";
                    }
                    storePositionButton.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
                    storePositionButton.FlashNumber = 2;
                }
                if (limbComboBox.SelectedItem.ToString() == "leg")
                {
                    if (sideComboBox.SelectedItem.ToString() == "left")
                    {
                        DesiredPositionsLeftLeg = MotorSequences.ReturnDictionaryOfPositions(Limbic.LeftLeg);
                        toolStripStatusLabel.Text = "Motors added to desired positions dictionary (left leg).";
                    }
                    if (sideComboBox.SelectedItem.ToString() == "right")
                    {
                        DesiredPositionsRightLeg = MotorSequences.ReturnDictionaryOfPositions(Limbic.RightLeg);
                        toolStripStatusLabel.Text = "Motors added to desired positions dictionary (right leg).";
                    }
                    storePositionButton.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
                    storePositionButton.FlashNumber = 2;
                }
            }
            //StoredPositions.StorePosition("arm_animation", DesiredPositions);
        }
        private void LiftLegButtonClick(object sender, EventArgs e)
        {
            // Replay.
            if (limbComboBox.SelectedItem.ToString() == "leg" & sideComboBox.SelectedItem.ToString() == "left")
            {
                // 1. Set to the first known set of positions of the leg. Torque has already been set.
                MotorControl.MoveMotorSequence(CurrentPositionsLeftLeg);
                Thread.Sleep(StepDelay);
                // 2. Move to the new desired set of positions of the leg.
                MotorControl.MoveMotorSequence(DesiredPositionsLeftLeg);
                Thread.Sleep(StepDelay);
                // 3. Return to the first known set of positions of the leg. Set torque off.
                MotorControl.MoveMotorSequence(CurrentPositionsLeftLeg);
                Thread.Sleep(StepDelay);
                MotorControl.SetTorqueOff(Limbic.LeftArm);
                MotorControl.SetTorqueOff(Limbic.Abdomen);
                MotorControl.SetTorqueOff(Limbic.RightArm);
                MotorControl.SetTorqueOff(Limbic.Head);
                MotorControl.SetTorqueOff(Limbic.Bust);
                toolStripStatusLabel.Text = "Torque released.";
                torqueStatusIndicator.FlasherButtonColorOn = Color.DeepPink;
            }
            if (limbComboBox.SelectedItem.ToString() == "leg" & sideComboBox.SelectedItem.ToString() == "right")
            {
                // 1. Set to the first known set of positions of the leg. Torque has already been set.
                MotorControl.MoveMotorSequence(CurrentPositionsRightLeg);
                Thread.Sleep(StepDelay);
                // 2. Move to the new desired set of positions of the leg.
                MotorControl.MoveMotorSequence(DesiredPositionsRightLeg);
                Thread.Sleep(StepDelay);
                // 3. Return to the first known set of positions of the leg. Set torque off.
                MotorControl.MoveMotorSequence(CurrentPositionsRightLeg);
                Thread.Sleep(StepDelay);
                MotorControl.SetTorqueOff(Limbic.LeftArm);
                MotorControl.SetTorqueOff(Limbic.Abdomen);
                MotorControl.SetTorqueOff(Limbic.RightArm);
                MotorControl.SetTorqueOff(Limbic.Head);
                MotorControl.SetTorqueOff(Limbic.Bust);
                toolStripStatusLabel.Text = "Torque released.";
                torqueStatusIndicator.FlasherButtonColorOn = Color.DeepPink;
            }
        }
        private void TurnHeadButtonClick(object sender, EventArgs e)
        {

        }
        private void ThreeStepsForwardButtonClick(object sender, EventArgs e)
        {

        }
        private void ThreeStepsBackwardButtonClick(object sender, EventArgs e)
        {

        }
        private void TorqueStatusIndicatorClick(object sender, EventArgs e)
        {
            torqueStatusIndicator.FlasherButtonStop();
        }
        private void ViewRelationalTableButtonClick(object sender, EventArgs e)
        {
            if (RelationalTable.Instance == false)
            {
                RelationalTable form = new RelationalTable(this);
                form.Show(this);
                RelationalTable.Instance = true;
            }
            else if (RelationalTable.Instance == true)
            {
                // Do nothing.
            }
        }
        private void Ax12ButtonClick(object sender, EventArgs e)
        {
            if (Ax12ControlTable.Instance == false)
            {
                Ax12ControlTable form = new Ax12ControlTable(this);
                form.Show(this);
                Ax12ControlTable.Instance = true;
            }
            else if (Ax12ControlTable.Instance == true)
            {
                // Do nothing.
            }
        }
        private void Ax18ButtonClick(object sender, EventArgs e)
        {
            if (Ax18ControlTable.Instance == false)
            {
                Ax18ControlTable form = new Ax18ControlTable(this);
                form.Show(this);
                Ax18ControlTable.Instance = true;
            }
            else if (Ax18ControlTable.Instance == true)
            {
                // Do nothing.
            }
        }
        private void MxButtonClick(object sender, EventArgs e)
        {
            if (MxControlTable.Instance == false)
            {
                MxControlTable form = new MxControlTable(this);
                form.Show(this);
                MxControlTable.Instance = true;
            }
            else if (MxControlTable.Instance == true)
            {
                // Do nothing.
            }
        }
        private void ViewMotorsButtonClick(object sender, EventArgs e)
        {
            if (MotorsRobot.Instance == false)
            {
                MotorsRobot form = new MotorsRobot(this);
                form.Show(this);
                MotorsRobot.Instance = true;
            }
            else if (MotorsRobot.Instance == true)
            {
                // Do nothing.
            }
        }
        private void MonitorSerialButtonClick(object sender, EventArgs e)
        {
            SerialPortThread = new Thread(() => PollSerialPort());
            SerialPortThread.Start();
        }
        private void CeaseSerialMonitorButtonClick(object sender, EventArgs e)
        {
            InvokeControl("", "Serial port disconnected.");
            SerialPort.Close();
            SerialPort.Dispose();
        }
        private void ClearMonitorButtonClick(object sender, EventArgs e)
        {
            xcPositionLabel.Text = "clr";
            ycPositionLabel.Text = "clr";
            zcPositionLabel.Text = "clr";
            xsPositionLabel.Text = "clr";
            ysPositionLabel.Text = "clr";
            zsPositionLabel.Text = "clr";
            xwPositionLabel.Text = "clr";
            ywPositionLabel.Text = "clr";
            zwPositionLabel.Text = "clr";
            logWindow.Text = "";
            logWindow.Refresh();
        }
        private void ClearLogButtonClick(object sender, EventArgs e)
        {
            logWindow.Text = string.Empty;
            logWindow.Refresh();
        }
        private void EngageDictionaryRoutineClick(object sender, EventArgs e)
        {
            // Parse the dictionary as a series of successive commands to the robot.
            //MotorFunctions.RobotMotorSequenceAction = RobotMotorSequenceAction;
            //serialMonitorBox.Text += 
            MotorControl.MoveMotorSequence();
        }
        private void LimbSelectionBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (limbComboBox.SelectedItem.ToString() == "arm")
            {
                raiseArmButton.Enabled = true;
                liftLegButton.Enabled = false;
            }  
            if (limbComboBox.SelectedItem.ToString() == "leg")
            {
                raiseArmButton.Enabled = false;
                liftLegButton.Enabled = true;
            }
        }
        private void SideComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (sideComboBox.SelectedItem.ToString() == "left" & limbComboBox.SelectedItem.ToString() == "arm")
            {
                MotorControl.SetTorqueOff(Limbic.LeftArm);
                MotorControl.SetTorqueOn(Limbic.Abdomen);
                MotorControl.SetTorqueOn(Limbic.RightArm);
                MotorControl.SetTorqueOn(Limbic.Head);
                MotorControl.SetTorqueOn(Limbic.Bust);
                TriggerTorqueIndicator(true, "Torque engaged.");
            }
            if (sideComboBox.SelectedItem.ToString() == "right" & limbComboBox.SelectedItem.ToString() == "arm")
            {
                MotorControl.SetTorqueOff(Limbic.RightArm);
                MotorControl.SetTorqueOn(Limbic.Abdomen);
                MotorControl.SetTorqueOn(Limbic.LeftArm);
                MotorControl.SetTorqueOn(Limbic.Head);
                MotorControl.SetTorqueOn(Limbic.Bust);
                TriggerTorqueIndicator(true, "Torque engaged.");
            }
            if (sideComboBox.SelectedItem.ToString() == "left" & limbComboBox.SelectedItem.ToString() == "leg")
            {
                MotorControl.SetTorqueOff(Limbic.LeftLeg);
            }
            if (sideComboBox.SelectedItem.ToString() == "right" & limbComboBox.SelectedItem.ToString() == "leg")
            {
                MotorControl.SetTorqueOff(Limbic.RightLeg);
            }
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Dynamixel.closePort(MotorFunctions.PortNumberUpper);
            //Dynamixel.closePort(MotorFunctions.PortNumberLower);
        }
        #endregion

    }
}
