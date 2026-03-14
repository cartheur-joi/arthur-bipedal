using Logging = Cartheur.Animals.Robot.Logging;
using Cartheur.Animals.Robot;
using DynamixelWizard.Controls;
using System.Media;

namespace DynamixelWizard.SubForms
{
    public partial class AnimationTraining : Form
    {
        string[] newlineDelimiter = { "\n" };
        string[] valueDelimiter = { "--" };
        private static SoundPlayer Countdown { get; set; }
        private static SoundPlayer Training { get; set; }

        public static bool Instance { get; set; }
        public string TrainingSelection { get; set; }
        public MotorFunctions MotorControl { get; set; }
        public MotorSequence MotorSequences { get; set; }
        public Remember RememberThings { get; set; }
        public Remember TrainingSequence { get; set; }
        public Remember StoredPositions { get; set; }
        public System.Windows.Forms.Timer TrainingStepTimer { get; set; }
        public System.Windows.Forms.Timer CountdownTimer { get; set; }
        public int TrainingStepTimeRemaining { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public int NumberOfTrainingSteps { get; set; }
        public int TrainingStepNumber { get; set; }
        public Dictionary<string, int> ReplayDictionary { get; set; }

        public AnimationTraining()
        {
            InitializeComponent();
            ReplayCheckBox.Checked = false;
            StepIntervalBox.TextAlign = HorizontalAlignment.Right;
            TimerCountdownBox.TextAlign = HorizontalAlignment.Right;
            var toolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 400, ReshowDelay = 250, ShowAlways = true };
            toolTip.SetToolTip(StepIntervalBox, "Type the amount of time for each step during recording (in seconds). This is the recording resolution, or how many individual positions any motor in the set will display. The shorter the duration, the more positions recorded.");
            toolTip.SetToolTip(RecordTrainingFlashButton, "Set the training selection in the Pose Hardening listbox first.");
            toolTip.SetToolTip(TimerControlButton, "Click to start the training time. Will flash red when training time has expired.");
            toolTip.SetToolTip(TimerCountdownBox, "Indicates the ticking countdown of the training time.");
            MotorControl = new MotorFunctions();
            MotorSequences = new MotorSequence();
            RememberThings = new Remember();
            TrainingSequence = new Remember(@"\db\trainings.db");
            StoredPositions = new Remember(@"\db\positions.db");
            TrainingStepTimer = new System.Windows.Forms.Timer
            {
                Interval = Convert.ToInt32(StepIntervalBox.Text) * 100
            };
            CountdownTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            TrainingStepTimer.Tick += RecordProcess;
            CountdownTimer.Tick += CountdownTimerDisplay;
            NumberOfTrainingSteps = 12;
            TrainingStepNumber = 1;
            Countdown = new SoundPlayer(Environment.CurrentDirectory + @"\sounds\countdown-alert.wav");
            Training = new SoundPlayer(Environment.CurrentDirectory + @"\sounds\training.wav");
            ReplayDictionary = new Dictionary<string, int>();
        }
        void PrepareTimer()
        {
            TrainingStepTimeRemaining = Convert.ToInt32(StepIntervalBox.Text);
            CountdownTimer.Start();
        }
        void FlashUnderstood()
        {
            understoodFlash.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
            understoodFlash.FlashNumber = 3;
        }
        void FlashNotUnderstood()
        {
            notUnderstoodFlash.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
            notUnderstoodFlash.FlashNumber = 3;
        }
 
        public void SaveDictionary(Dictionary<string, int> dictionary, Limbic.LimbicArea area)
        {
            string filePath = Environment.CurrentDirectory + @"\logs\" + area.ToString() + ".txt";
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (TextWriter tw = new StreamWriter(fs))

                    foreach (KeyValuePair<string, int> kvp in dictionary)
                    {
                        tw.WriteLine(string.Format("{0}--{1}", kvp.Key, kvp.Value));
                    }
            }
            notificationLabel.Text = "File saved.";
        }
        public void DisplayMemory(Dictionary<string, int> dictionary)
        {
            var lines = dictionary.Select(kv => kv.Key + "--" + kv.Value.ToString());
            memoryContainsTextBox.Text = string.Join(Environment.NewLine, lines);
        }
        public void CreateDictionary()
        {
            // Create an animation dictionary from the entry.
            foreach (var line in memoryContainsTextBox.Text.Split(newlineDelimiter, StringSplitOptions.RemoveEmptyEntries))
            {
                var sl = line.Split(newlineDelimiter, StringSplitOptions.RemoveEmptyEntries);
                string[] sp = line.Split(valueDelimiter, StringSplitOptions.RemoveEmptyEntries);
                RememberThings.Positions.Add(sp[0], Convert.ToUInt16(sp[1]));
            }
            notificationLabel.Text = "Dictionary created.";
        }

        #region Events

        private void CloseButtonClick(object sender, EventArgs e)
        {
            Instance = false;
            Close();
        }
        private void ResetPositionsButton_Click(object sender, EventArgs e)
        {
            if (MotorSequence.MotorArrayOfInterest.Count > 0)
            {
                MotorControl.MoveMotorSequence(MotorSequence.MotorArrayOfInterest);
            }
            FlashUnderstood();
        }
        private void LearnThisCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            TrainingSelection = poseHardeningSelectionBox.SelectedItem.ToString();

        }
        private void ShowInMemeoryButtonClick(object sender, EventArgs e)
        {
            foreach (var entry in MotorSequence.MotorArraysOfInterest) 
            {
                DisplayMemory(entry);
            }
        }
        private void SaveCopyButtonClick(object sender, EventArgs e)
        {
            memoryCopyTextBox.Text += memoryContainsTextBox.Text + Environment.NewLine;
        }
        private void StoreInDbButtonClick(object sender, EventArgs e)
        {
            Remember.VerboseCommand = "Extend your left arm in front of you";
            CreateDictionary();
            RememberThings.StoreMemory(memoryContainsTextBox.Text);
        }
        private void PlayAnimationButtonClick(object sender, EventArgs e)
        {
            //MotorControl.MoveMotorSequence(SilentForm.AnimationDictionary);
        }
        private void RevertAnimationButtonClick(object sender, EventArgs e)
        {
            //MotorControl.MoveMotorSequence(SilentForm.AnimationDictionary);
        }
        private void LimbSelectionBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            TrainingSelection = limbSelectionBox.SelectedItem.ToString();
            // If it is only necessary to untorque set motors.
            if (releaseCheckBox.Checked)
            {
                switch (TrainingSelection)
                {
                    case "Left leg":
                        // Untorque those motors for training/manual placement.
                        MotorControl.SetTorqueOff(Limbic.LeftLeg);
                        break;
                    case "Right leg":
                        MotorControl.SetTorqueOff(Limbic.RightLeg);
                        break;
                    default:
                        return;
                }
            }
            if (setPositionCheckBox.Checked)
            {
                // Check if the dictionary already exists and clear it if it does.
                if (!compositeListCheckBox.Checked)
                {
                    if (MotorSequence.MotorArrayOfInterest.Count > 0)
                        MotorSequence.MotorArrayOfInterest.Clear();
                }
                // Teach the robot what positions to learn. Note correction by reinforement learning (partially-implemented in Boagaphish).
                // Create dictionaries of the current positions of all motors. Move to Remember.Initialize()?
                switch (TrainingSelection)
                {
                    case "Abdomen":
                        MotorSequences.CreateDictionaryOfPositions(Limbic.Abdomen);
                        MotorSequence.MotorArraysOfInterest.Add(MotorSequence.MotorArrayOfInterest);
                        notificationLabel.Text = "Motors added to training dictionary (abdomen).";
                        break;
                    case "Bust":
                        MotorSequences.CreateDictionaryOfPositions(Limbic.Bust);
                        MotorSequence.MotorArraysOfInterest.Add(MotorSequence.MotorArrayOfInterest);
                        notificationLabel.Text = "Motors added to training dictionary (bust).";
                        break;
                    case "Head":
                        MotorSequences.CreateDictionaryOfPositions(Limbic.Head);
                        MotorSequence.MotorArraysOfInterest.Add(MotorSequence.MotorArrayOfInterest);
                        notificationLabel.Text = "Motors added to training dictionary (head).";
                        break;
                    case "Left arm":
                        MotorSequences.CreateDictionaryOfPositions(Limbic.LeftArm);
                        MotorSequence.MotorArraysOfInterest.Add(MotorSequence.MotorArrayOfInterest);
                        notificationLabel.Text = "Motors added to training dictionary (left arm).";
                        break;
                    case "Right arm":
                        MotorSequences.CreateDictionaryOfPositions(Limbic.RightArm);
                        MotorSequence.MotorArraysOfInterest.Add(MotorSequence.MotorArrayOfInterest);
                        notificationLabel.Text = "Motors added to training dictionary (right arm).";
                        break;
                    case "Left leg":
                        MotorSequences.CreateDictionaryOfPositions(Limbic.LeftLeg);
                        MotorSequence.MotorArraysOfInterest.Add(MotorSequence.MotorArrayOfInterest);
                        notificationLabel.Text = "Motors added to training dictionary (left leg).";
                        break;
                    case "Right leg":
                        MotorSequences.CreateDictionaryOfPositions(Limbic.RightLeg);
                        MotorSequence.MotorArraysOfInterest.Add(MotorSequence.MotorArrayOfInterest);
                        notificationLabel.Text = "Dictionary created and added (right leg).";
                        break;
                    default:
                        return;
                }
                FlashUnderstood();
            }
            else
            {
                notificationLabel.Text = "Select an operation to perform upon selection.";
                FlashNotUnderstood();
            }
        }
        private void PoseHardeningSelectionBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            TrainingSelection = poseHardeningSelectionBox.SelectedItem.ToString();
        }
        private void RecordTrainingFlashButtonClick(object sender, EventArgs e)
        {
            // Sound testing.
            //Countdown.PlaySync();
            //Training.PlaySync();
            if (TrainingSelection == null)
            {
                RecordTrainingFlashButton.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
                RecordTrainingFlashButton.FlashNumber = 1;
                RecordTrainingFlashButton.BackColor = Color.MediumVioletRed;
            }
            if (TrainingSelection != null)
            {
                switch (TrainingSelection)
                {
                    case "Left arm":
                        //RecordAnimation(Limbic.LeftArm);
                        break;
                    case "Left leg":
                        //RecordAnimation(Limbic.LeftLeg);
                        break;
                    case "Right arm":
                        //RecordAnimation(Limbic.RightArm);
                        break;
                    case "Right leg":
                        //RecordAnimation(Limbic.RightLeg);
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region Recorded training session managemeent

        public void RecordAnimation(string[] trainingAnimation, int timeStep)
        {
            // Record the animations on each tick event.
            // Collect the current positions of all motors, store them as a dictionary<string,int> object, and write them into a file.
            MotorSequences.ReturnDictionaryOfPositions(trainingAnimation).StoreMotorSequenceAsFile(Environment.CurrentDirectory + @"\animations\animationDictionary" + timeStep + ".txt");

            #region old notes
            // Start a countdown timer - only executes once. Perhaps a tone to indicate when the countdown is nearly finished?
            //CountdownTimer.Start();
            // Record the hand-moved motion into the database - only executes once. Start sequence timer - duration 10 seconds.
            // Running in AlertCountdownReached.
            // Revert the motion to where it was at the beginning - in the postions database.
            // Running on line 290.
            // Save these two (trainings) as "step forward <right leg>" (as in walking).
            // No. This is a higher abstraction, as started on the TemplaterForm.
            // Replay the demoed motion AND its return to the stability point.
            // Again. A higher abstraction but is worthy to implement for this application.
            #endregion
        }
        public void ReplayAnimation(int timeStep)
        {
            // Replay the animations on each tick event. Maybe a good idea is another method that plays the entire sequence but timing is the question.
            // Create a dictionary for the stepwise play sequence from the files.
            try
            {
                ReplayDictionary = MotorSequences.BuildMotorSequence(Environment.CurrentDirectory + @"\animations\animationDictionary" + timeStep + ".txt");
            }
            catch (Exception ex)
            {
                Cartheur.Animals.Robot.Logging.WriteLog(ex.Message, Logging.LogType.Error, Logging.LogCaller.Demo);
            }
            // Play it back.
            MotorControl.MoveMotorSequence(ReplayDictionary);
            
        }
        void CountdownTimerDisplay(object sender, EventArgs e)
        {
            if (ReplayCheckBox.Checked)
            {
                if (TrainingStepTimeRemaining == 0)
                {
                    CountdownTimer.Stop();
                    TimerControlButton.FlasherButtonColorOn = Color.Red;
                    TimerControlButton.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
                    TimerControlButton.FlashNumber = 3;
                    Training.Play();
                }
                if (TrainingStepTimeRemaining == 5)
                {
                    TrainingStepTimeRemaining--;
                    // Replay the sequence of animations from 5 down to zero.
                    ReplayAnimation(TrainingStepTimeRemaining);
                    BeginInvoke(new MethodInvoker(() => TimerCountdownBox.Text = TrainingStepTimeRemaining.ToString()));
                    Countdown.Play();
                }
                else
                {
                    TrainingStepTimeRemaining--;
                    // Replay the sequence of animations from the timer value down to 6.
                    ReplayAnimation(TrainingStepTimeRemaining);
                    BeginInvoke(new MethodInvoker(() => TimerCountdownBox.Text = TrainingStepTimeRemaining.ToString()));
                }
            }
            if (TrainingStepTimeRemaining == 0)
            {
                CountdownTimer.Stop();
                TimerControlButton.FlasherButtonColorOn = Color.Red;
                TimerControlButton.FlasherButtonStart(FlashIntervalSpeed.FlashFiniteSlow);
                TimerControlButton.FlashNumber = 3;
                Training.Play();
            }
            if (TrainingStepTimeRemaining == 5)
            {
                TrainingStepTimeRemaining--;
                // Record any and all animations for the timer period using all motors from david.
                RecordAnimation(Limbic.All, TrainingStepTimeRemaining);
                BeginInvoke(new MethodInvoker(() => TimerCountdownBox.Text = TrainingStepTimeRemaining.ToString()));
                Countdown.Play();
            }
            else
            {
                TrainingStepTimeRemaining--;
                // Record any and all animations for the timer period using all motors from david.
                RecordAnimation(Limbic.All, TrainingStepTimeRemaining);
                BeginInvoke(new MethodInvoker(() => TimerCountdownBox.Text = TrainingStepTimeRemaining.ToString())); 
            }
        }
        /// <summary>
        /// Records the process.
        /// </summary>
        /// <remarks>Every second the presented animation of a particular limb are recorded in the training database as a sequence that should be repeated.</remarks>
        void RecordProcess(object sender, EventArgs e)
        {
            switch (TrainingSelection)
            {
                case "Left arm":
                    //RecordAnimation(Limbic.LeftArm);

                    MotorSequences.CreatePositTrainingSelection(TrainingStepNumber, TrainingSelection, Limbic.LeftArm, TrainingSequence);
                    break;
                case "Left leg":
                    MotorSequences.CreatePositTrainingSelection(TrainingStepNumber, TrainingSelection, Limbic.LeftLeg, TrainingSequence);
                    break;
                case "Right arm":
                    MotorSequences.CreatePositTrainingSelection(TrainingStepNumber, TrainingSelection, Limbic.RightArm, TrainingSequence);
                    break;
                case "Right leg":
                    MotorSequences.CreatePositTrainingSelection(TrainingStepNumber, TrainingSelection, Limbic.RightLeg, TrainingSequence);
                    break;
                default:
                    break;
            }
            TrainingStepNumber++;
            if (TrainingStepNumber == NumberOfTrainingSteps)
            {
                TrainingStepTimer.Stop();
                // Training is ending.
                Training.Play(); // Three seconds.
                RevertLimb();
            }
        }
        public void RevertLimb()
        {
            switch (TrainingSelection)
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

        private void TimerControlButtonClick(object sender, EventArgs e)
        {
            PrepareTimer();
            TimerControlButton.FlasherButtonColorOff = SystemColors.Control;
            // Set a five second delay so that the operator can be at the robot when recording begins.
            Training.Play();
            TimerControlButton.FlasherButtonColorOn = Color.Tomato;
            
        }

        #region boneyard
        //void RecordProcess(object sender, EventArgs e)
        //{
        //    switch (TrainingSelection)
        //    {
        //        case "Left arm":
        //            MotorSequences.CreatePositTrainingSelection(TrainingStepNumber, TrainingSelection, Limbic.LeftArm, TrainingSequence);
        //            break;
        //        case "Left leg":
        //            MotorSequences.CreatePositTrainingSelection(TrainingStepNumber, TrainingSelection, Limbic.LeftLeg, TrainingSequence);
        //            break;
        //        case "Right arm":
        //            MotorSequences.CreatePositTrainingSelection(TrainingStepNumber, TrainingSelection, Limbic.RightArm, TrainingSequence);
        //            break;
        //        case "Right leg":
        //            MotorSequences.CreatePositTrainingSelection(TrainingStepNumber, TrainingSelection, Limbic.RightLeg, TrainingSequence);
        //            break;
        //        default:
        //            break;
        //    }
        //    TrainingStepNumber++;
        //    if (TrainingStepNumber == NumberOfTrainingSteps)
        //    {
        //        TrainingStepTimer.Stop();
        //        // Training is ending.
        //        Training.Play(); // Three seconds.
        //        RevertLimb();
        //    }
        //}
        #endregion
    }
}
