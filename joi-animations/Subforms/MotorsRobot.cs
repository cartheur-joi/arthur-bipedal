namespace DynamixelWizard.SubForms
{
    public partial class MotorsRobot : Form
    {
        private Form owner;
        private static bool instance = false;

        public MotorsRobot(Form mOwner)
        {
            InitializeComponent();
            owner = mOwner;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            instance = true;
        }
        /// <summary>
        /// Checks the instance of the form.
        /// </summary>
        public static bool Instance { get { return instance; } set { instance = value; } }
        /// <summary>
        /// Closes the window.
        /// </summary>
        private void CloseWindowButtonClick(object sender, EventArgs e)
        {
            instance = false;
            Close();
        }
    }
}
