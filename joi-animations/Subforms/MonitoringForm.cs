using Timer = System.Windows.Forms.Timer;

namespace DynamixelWizard.SubForms
{
    public partial class MonitoringForm : Form
    {
        public static bool Instance { get; set; }
        public Timer MonitorTimer { get; set; }

        public MonitoringForm()
        {
            InitializeComponent();
            MonitorTimer = new Timer()
            {
                Interval = 1000
            };
        }
        /// <summary>
        /// Monitors the currents on all motors and prints them to the form, according to the placed boxes relative to the motor assignments.
        /// </summary>
        public void MonitorCurrents()
        {

        }
        /// <summary>
        /// Will communicate to the form if a motor is overloaded.
        /// </summary>
        public void ShowOverload()
        {

        }

        #region Events

        private void CloseButtonClick(object sender, System.EventArgs e)
        {
            Instance = false;
            Close();
        }
        private void StartMonitoringButtonClick(object sender, System.EventArgs e)
        {
            MonitorCurrents();
        }
        private void StopMonitoringButtonClick(object sender, System.EventArgs e)
        {

        }
        #endregion
    }
}
