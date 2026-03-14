using Cartheur.Animals.Robot;
using System.IO.Ports;
using System.Timers;

namespace DynamixelWizard.SubForms
{
    public partial class ControlKeypad : Form
    {
        const string ArduinoPort = "COM6";
        public static SerialPort SerialPort { get; set; }
        public static bool Instance { get; set; }
        public MotorFunctions MotorControl { get; set; }
        public MotorSequence MotorSequences { get; set; }
        public Remember RememberThings { get; set; }

        #region Gyroscopic data
        public List<double> X { get; set; }
        public List<double> Y { get; set; }
        public List<double> Z { get; set; }
        public int Iteration { get; set; }
        public bool Reading { get; set; }
        #endregion

        #region Time-series gyroscopic data
        public Dictionary<double, long> XSeries { get; set; }
        public Dictionary<double, long> YSeries { get; set; }
        public Dictionary<double, long> ZSeries { get; set; }
        #endregion

        #region The stability trig-point feature

        public bool Stability { get; set; }
        public System.Timers.Timer BalancingPoll { get; set; }

        #endregion

        public ControlKeypad()
        {
            InitializeComponent();
            KeyPreview = true;
            var toolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 400, ReshowDelay = 250, ShowAlways = true };
            toolTip.SetToolTip(LeftAnkleFlashButton, "Controlled by key 'N'.");
            toolTip.SetToolTip(RightAnkleFlashButton, "Controlled by key 'B'.");
            toolTip.SetToolTip(LeftPelvisFlashButton, "Controlled by key 'D'.");
            toolTip.SetToolTip(RightPelvisFlashButton, "Controlled by key 'A'.");
            toolTip.SetToolTip(abdomenFlashButton, "Controlled by key 'X'.");
            toolTip.SetToolTip(leftLegFlashButton, "Controlled by key 'C'.");
            toolTip.SetToolTip(bustFlashButton, "Controlled by key 'S'.");
            toolTip.SetToolTip(rightLegFlashButton, "Controlled by key 'Z'.");
            toolTip.SetToolTip(headFlashButton, "Controlled by key 'W'.");
            toolTip.SetToolTip(leftArmFlashButton, "Controlled by key 'E'.");
            toolTip.SetToolTip(rightArmFlashButton, "Controlled by key 'Q'.");
            toolTip.SetToolTip(CloseButton, "Controlled by key 'V'.");
            MotorControl = new MotorFunctions();
            MotorSequences = new MotorSequence();
            RememberThings = new Remember(@"\db\positions.db");
            X = new List<double>();
            Y = new List<double>();
            Z = new List<double>();
            XSeries = new Dictionary<double, long>();
            YSeries = new Dictionary<double, long>();
            ZSeries = new Dictionary<double, long>();

            SerialPort = new SerialPort
            {
                PortName = ArduinoPort,
                BaudRate = 115200
            };

            //BalancingPoll = new System.Timers.Timer();
            //BalancingPoll.Elapsed += PollStableBalancing; // Throws a non-negative number error.
            // Arbitrary as how fast need it react to disturabances that could result in falling?
            // However, due to the workflow this value yields a stable application if less than 2000. Needs work.
            //BalancingPoll.Interval = 2000;
            //BalancingPoll.Start();
            //SerialPort.Open();
            //StabilityAchievedFlashButton.Enabled = false;
            //Reading = false;
            ReadMotorStatus();

            notificationsLabel.Text = "---";
        }

        #region Keypad Feature-Set

        // For this feature: Click is torque-on, button colored. Click is torque-off.
        private void LeftLegFlashButtonClick(object sender, EventArgs e)
        {
            if (leftLegFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.LeftLegNoPelvis);
                leftLegFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Left leg released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.LeftLegNoPelvis);
                notificationsLabel.Text = "Left leg applied.";
                leftLegFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void LeftPelvisFlashButtonClick(object sender, EventArgs e)
        {
            if (LeftPelvisFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.LeftPelvis);
                LeftPelvisFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Left pelvis released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.LeftPelvis);
                notificationsLabel.Text = "Left pelvis applied.";
                LeftPelvisFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void LeftArmFlashButtonClick(object sender, EventArgs e)
        {
            if (leftArmFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.LeftArm);
                leftArmFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Left arm released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.LeftArm);
                notificationsLabel.Text = "Left arm applied.";
                leftArmFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void LeftAnkleFlashButtonClick(object sender, EventArgs e)
        {
            if (LeftAnkleFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.LeftAnkle);
                LeftAnkleFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Left ankle released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.LeftAnkle);
                notificationsLabel.Text = "Left ankle applied.";
                LeftAnkleFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void AbdomenFlashButtonClick(object sender, EventArgs e)
        {
            if (abdomenFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.Abdomen);
                abdomenFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Abdomen released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.Abdomen);
                notificationsLabel.Text = "Abdomen applied.";
                abdomenFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void BustFlashButtonClick(object sender, EventArgs e)
        {
            if (bustFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.Bust);
                bustFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Bust released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.Bust);
                notificationsLabel.Text = "Bust applied.";
                bustFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void HeadFlashButtonClick(object sender, EventArgs e)
        {
            if (headFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.Head);
                headFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Head released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.Head);
                notificationsLabel.Text = "Head applied.";
                headFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void RightLegFlashButtonClick(object sender, EventArgs e)
        {
            if (rightLegFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.RightLegNoPelvis);
                rightLegFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Right leg released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.RightLegNoPelvis);
                notificationsLabel.Text = "Right leg applied.";
                rightLegFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void RightPelvisFlashButtonClick(object sender, EventArgs e)
        {
            if (RightPelvisFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.RightPelvis);
                RightPelvisFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Right pelvis released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.RightPelvis);
                notificationsLabel.Text = "Right pelvis applied.";
                RightPelvisFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void RightArmFlashButtonClick(object sender, EventArgs e)
        {
            if (rightArmFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.RightArm);
                rightArmFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Right arm released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.RightArm);
                notificationsLabel.Text = "Right arm applied.";
                rightArmFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void RightAnkleFlashButtonClick(object sender, EventArgs e)
        {
            if (RightAnkleFlashButton.BackColor == Color.LightGreen)
            {
                MotorControl.SetTorqueOff(Limbic.RightAnkle);
                RightAnkleFlashButton.BackColor = SystemColors.Control;
                notificationsLabel.Text = "Right ankle released.";
            }
            else
            {
                MotorControl.SetTorqueOn(Limbic.RightAnkle);
                notificationsLabel.Text = "Right ankle applied.";
                RightAnkleFlashButton.BackColor = Color.LightGreen;
            }
        }
        private void CapturePositionsFlashButtonClick(object sender, EventArgs e)
        {
            // Perform a check that the database according to position type does not have the data. Not fully implemented.
            if (RememberThings.RetrieveData("StablePosition").Tables[0].Rows.Count > 0)
            {
                notificationsLabel.Text = "Database populated.";
                CapturePositionsFlashButton.BackColor = Color.MediumVioletRed;
                return;
            }
            // Capture the set of positions and store them in the positions.db (in the StablePositions table).
            // This will serve as a set of goal positions later.
            if (RememberThings.StorePosition("Standing", MotorControl.GetPresentPositions(Limbic.All)))
                notificationsLabel.Text = "The array of positions has been stored in the database.";
            CapturePositionsFlashButton.BackColor = Color.MediumVioletRed;
            ClearDatabaseFlashButton.BackColor = Color.LightGreen;
        }

        #endregion

        #region Gyroscopic Balance Feature-Set
        // Note: 
        void PollStableBalancing(object source, ElapsedEventArgs e)
        {
            if(!Reading)
            {
                ParseCoordinates(SerialPort.ReadExisting());
                if (X[Iteration] >= -5.00 && X[Iteration] <= 5.00 && Y[Iteration] >= -5.00 && Y[Iteration] <= 5.00 && Z[Iteration] >= -5.00 && Z[Iteration] <= 5.00)
                    StabilityAchieved(true);
                else
                    StabilityAchieved(false);
                Reading = false;
            }
            
        }
        void PrintGyroscopicValues(string value)
        {
            if (xPositionLabel.InvokeRequired)
            {
                xPositionLabel.Invoke(new MethodInvoker(delegate { Name = "x-position"; }));
                this.Invoke(new MethodInvoker(delegate {
                    xPositionLabel.Text = value;
                }));
            }
            if (yPositionLabel.InvokeRequired)
            {
                yPositionLabel.Invoke(new MethodInvoker(delegate { Name = "y-position"; }));
                this.Invoke(new MethodInvoker(delegate {
                    yPositionLabel.Text = value;
                }));
            }
            if (zPositionLabel.InvokeRequired)
            {
                zPositionLabel.Invoke(new MethodInvoker(delegate { Name = "z-position"; }));
                this.Invoke(new MethodInvoker(delegate {
                    zPositionLabel.Text = value;
                    Refresh();
                }));
            }
        }
        void StabilityAchieved(bool achieved)
        {
            if (StabilityAchievedFlashButton.InvokeRequired)
            {
                StabilityAchievedFlashButton.Invoke(new MethodInvoker(delegate { Name = "Stability Flash"; }));
                this.Invoke(new MethodInvoker(delegate {
                    if(achieved) 
                        StabilityAchievedFlashButton.BackColor = Color.LightGreen;
                    if(!achieved)
                        StabilityAchievedFlashButton.BackColor = Color.MediumVioletRed;

                }));
            }
        }
        public void ParseCoordinates(string input)
        {
            Reading = true;
            if (input != "")
            {
                var coords = input.Split(new char[] { ';' });
                for (var i = 0; i < coords.Length; i++)
                {
                    var sum = coords[i].Split('=');
                    switch (sum[0].Trim())
                    {
                        case "X":
                            PrintGyroscopicValues(sum[1]);
                            X.Add(Convert.ToDouble(sum[1]));
                            //XSeries.Add(Convert.ToDouble(sum[1]), DateTime.UtcNow.Ticks);
                            break;
                        case "Y":
                            PrintGyroscopicValues(sum[1]);
                            Y.Add(Convert.ToDouble(sum[1]));
                            //YSeries.Add(Convert.ToDouble(sum[1]), DateTime.UtcNow.Ticks);
                            break;
                        case "Z":
                            PrintGyroscopicValues(sum[1]);
                            Z.Add(Convert.ToDouble(sum[1]));
                            //ZSeries.Add(Convert.ToDouble(sum[1]), DateTime.UtcNow.Ticks);
                            break;
                    }
                }
                Iteration++;
            }
        }

        #endregion

        #region Events
        private void CloseButtonClick(object sender, EventArgs e)
        {
            if (SerialPort != null) 
            {
                SerialPort.Close();
                SerialPort.Dispose();
            }
            Instance = false;
            Close();
        }
        private void ClearDatabaseFlashButtonClick(object sender, EventArgs e)
        {
            if (RememberThings.ClearTable("StablePosition"))
                notificationsLabel.Text = "The data in the positions database has been cleared.";
            CapturePositionsFlashButton.BackColor = Color.LightGreen;
            ClearDatabaseFlashButton.BackColor = SystemColors.Control;
        }
        private void RecallPositionFlashButtonClick(object sender, EventArgs e)
        {
            var ds = RememberThings.RetrieveData("StablePosition");
            if (ds.Tables[0].Rows.Count > 0)// will error if empty, needs fixing
                MotorControl.MoveMotorSequence(RememberThings.TransformPosition(ds));
            notificationsLabel.Text = "Last position recalled.";
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void ControlKeypadKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Q:
                    RightArmFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.W:
                    HeadFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.E:
                    LeftArmFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.A:
                    RightPelvisFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.S:
                    BustFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.D:
                    LeftPelvisFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.Z:
                    RightLegFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.X:
                    AbdomenFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.C:
                    LeftLegFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.V:
                    CloseButtonClick(sender, new EventArgs());
                    break;
                case Keys.N:
                    LeftAnkleFlashButtonClick(sender, new EventArgs());
                    break;
                case Keys.B:
                    RightAnkleFlashButtonClick(sender, new EventArgs());
                    break;
                default:
                    break;
            }
        }
        private void ReadMotorStatus()
        {
            if (MotorControl.IsTorqueOn(Limbic.LeftPelvis))
            {
                LeftPelvisFlashButton.BackColor = Color.LightGreen;
            }

                
        }
        #endregion

    }
}
