using Cartheur.Animals.Robot;

namespace DynamixelWizard.SubForms
{
    public partial class RobotControl : Form
    {
        string[] newlineDelimiter = { "\n" };
        string[] valueDelimiter = { "--" };
        const string FilePath = @"control-sequence.txt";

        public static bool Instance { get; set; }

        public MotorFunctions MotorControl { get; set; }
        public MotorSequence MotorSequences { get; set; }
        public Remember RememberThings { get; set; }

        public RobotControl()
        {
            InitializeComponent();
            MotorSequence.SequenceCommand = new Dictionary<string, int>();
            fileNameTextBox.Text = FilePath;
            MotorControl = new MotorFunctions();
            MotorSequences = new MotorSequence();
            RememberThings = new Remember();
            notificationLabel.Text = "ooo";
        }

        public void CreateDictionary()
        {
            foreach (var line in motorEngagemenetList.Text.Split(newlineDelimiter, StringSplitOptions.RemoveEmptyEntries))
            {
                var sl = line.Split(newlineDelimiter, StringSplitOptions.RemoveEmptyEntries);
                string[] sp = line.Split(valueDelimiter, StringSplitOptions.RemoveEmptyEntries);
                MotorSequence.SequenceCommand.Add(sp[0], Convert.ToUInt16(sp[1]));
            }
            notificationLabel.Text = "Dictionary created.";
        }
        public void SaveDictionary()
        {
            string filePath = Environment.CurrentDirectory + @"\logs\" + fileNameTextBox.Text;
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (TextWriter tw = new StreamWriter(fs))

                    foreach (KeyValuePair<string, int> kvp in MotorSequence.SequenceCommand)
                    {
                        tw.WriteLine(string.Format("{0}--{1}", kvp.Key, kvp.Value));
                    }
            }
            notificationLabel.Text = "File saved.";
        }
        public void LoadDictionary()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Environment.CurrentDirectory + @"\logs\" + FilePath))
                {
                    string _line;
                    while ((_line = sr.ReadLine()) != null)
                    {
                        string[] keyvalue = _line.Split(valueDelimiter, StringSplitOptions.RemoveEmptyEntries);
                        if (keyvalue.Length == 2)
                        {
                            MotorSequence.SequenceCommand.Add(keyvalue[0], Convert.ToUInt16(keyvalue[1]));
                        }
                    }
                }
                var lines = MotorSequence.SequenceCommand.Select(kv => kv.Key + "--" + kv.Value.ToString());
                motorEngagemenetList.Text = string.Join(Environment.NewLine, lines);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Loading error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            notificationLabel.Text = "Dictionary loaded.";
        }
        public void ClearDictionaries()
        {
            if (MotorSequence.MotorArrayOfInterest.Count > 0)
                MotorSequence.MotorArrayOfInterest.Clear();
        }

        #region Events

        private void CloseButtonClick(object sender, EventArgs e)
        {
            //TemplaterForm.RobotMotorSequenceAction = MotorSequence.SequenceCommand;
            Instance = false;
            Close();
        }
        private void AddToListButtonClick(object sender, EventArgs e)
        {
            if (goalPosition.Text != "")
                motorEngagemenetList.Text += motorSelectionList.SelectedItem + "--" + goalPosition.Text + Environment.NewLine;
            else
            {
                MessageBox.Show("Do not set position to zero.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            goalPosition.Text = string.Empty;
            motorSelectionList.SelectedItem = string.Empty;
        }
        private void EngageButtonClick(object sender, EventArgs e)
        {
            CreateDictionary();
        }
        private void SaveListButtonClick(object sender, EventArgs e)
        {
            SaveDictionary();
        }
        private void LoadDictionaryButtonClick(object sender, EventArgs e)
        {
            LoadDictionary();
        }
        private void ClearBoxButtonClick(object sender, EventArgs e)
        {
            motorEngagemenetList.Text = string.Empty;
            MotorSequence.SequenceCommand.Clear();
        }

        #endregion

    }
}
