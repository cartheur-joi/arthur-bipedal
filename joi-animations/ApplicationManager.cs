using Cartheur.Animals.Robot;
using DynamixelWizard.Controls;
using DynamixelWizard.SubForms;
using System.Timers;

namespace Cartheur.Animation.Joi
{
    public partial class ApplicationManager : Form
    {
        private System.Timers.Timer Counter {  get; set; }
        public System.Timers.Timer AliveTime { get; set; }
        public MotorFunctions MotorControl { get; set; }
        public bool MotorsInitialized { get; set; }
        public UsbCamera Camera { get; set; }
        public ApplicationManager()
        {
            InitializeComponent();
            KeyPreview = true;
            MotorControl = new MotorFunctions();
            NotificationLabel.Text = MotorControl.InitializeDynamixelMotors();
            MotorsInitialized = MotorFunctions.DynamixelMotorsInitialized;
            MotorFunctions.CollateMotorArray();

            if (MotorsInitialized) { MotorControl.CreateConnectMotorObjects(); }
            else NotificationLabel.Text = "Cannot create connection objects.";

            Counter = new System.Timers.Timer();
            Counter.Elapsed += AliveTimerElapsed;
            Counter.Interval = 1000;
            Counter.Start();
        }

        #region Events
        private void AliveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            PrintCounter();
        }
        private void PrintCounter()
        {
            if (timeDisplayBox.InvokeRequired)
            {
                timeDisplayBox.Invoke(new MethodInvoker(delegate { Name = timeDisplayBox.Text; }));
                Invoke(new MethodInvoker(delegate
                {
                    timeDisplayBox.Text = DateTime.Now.ToString("HH:mm:ss");
                }));
            }
            else
            {
                // Do nothing.
            }
        }
        private void ApplicationManagerKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.D1:
                    RobotControlFormButtonClick(sender, new EventArgs());
                    break;
                case Keys.D2:
                    AnimationTrainingButtonClick(sender, new EventArgs());
                    break;
                case Keys.D3:
                    ControlKeypadLaunchButtonClick(sender, new EventArgs());
                    break;
                case Keys.D4:
                    TemplaterLaunchButtonClick(sender, new EventArgs());
                    break;
                case Keys.V:
                    QuitApplicationButtonClick(sender, new EventArgs());
                    break;
                default:
                    break;
            }
        }
        private void QuitApplicationButtonClick(object sender, EventArgs e)
        { 
            Close();
        }
        private void RobotControlFormButtonClick(object sender, EventArgs e)
        {
            if (RobotControl.Instance == false)
            {
                RobotControl form = new RobotControl();
                form.Show(this);
                RobotControl.Instance = true;
            }
            else if (RobotControl.Instance == true)
            {
               // if (RobotMotorSequenceAction.Count != 0)
                   // notification.Text = "Dictionary in memory.";
            }

        }
        private void AnimationTrainingButtonClick(object sender, EventArgs e)
        {
            if (AnimationTraining.Instance == false)
            {
                AnimationTraining form = new AnimationTraining();
                form.Show(this);
                AnimationTraining.Instance = true;
            }
            else if (AnimationTraining.Instance == true)
            {
                // Do nothing.
            }
        }
        private void ControlKeypadLaunchButtonClick(object sender, EventArgs e)
        {
            if (ControlKeypad.Instance == false)
            {
                ControlKeypad form = new ControlKeypad();
                form.Show(this);
                ControlKeypad.Instance = true;
            }
            else if (ControlKeypad.Instance == true)
            {
                // Do nothing.
            }
        }
        private void TemplaterLaunchButtonClick(object sender, EventArgs e)
        {
            if (TemplaterForm.Instance == false)
            {
                TemplaterForm form = new TemplaterForm();
                form.Show(this);
                TemplaterForm.Instance = true;
            }
            else if (TemplaterForm.Instance == true)
            {
                // Do nothing.
            }
        }
        private void DemoFormButtonClick(object sender, EventArgs e)
        {
            if (DemosForm.Instance == false)
            {
                DemosForm form = new DemosForm();
                form.Show(this);
                DemosForm.Instance = true;
            }
            else if (DemosForm.Instance == true)
            {
                // Do nothing.
            }
        }
        private void MonitoringFormButtonClick(object sender, EventArgs e)
        {
            if (MonitoringForm.Instance == false)
            {
                MonitoringForm form = new MonitoringForm();
                form.Show(this);
                MonitoringForm.Instance = true;
            }
            else if (MonitoringForm.Instance == true)
            {
                // Do nothing.
            }
        }
        private void PopoutCameraButtonClick(object sender, EventArgs e)
        {
            if (PopoutCamera.Instance == false)
            {
                if(Camera != null)
                    Camera.Release();
                PopoutCamera form = new PopoutCamera();
                form.Show(this);
                PopoutCamera.Instance = true;
            }
            else if (PopoutCamera.Instance == true)
            {
                // Do nothing.
            }
        }
        private void StartCameraButtonClick(object sender, EventArgs e)
        {
            if(!PopoutCamera.IsCameraLocked)
            {
                var devices = UsbCamera.FindDevices();
                if (devices.Length == 0) return;
                var cameraIndex = 0;
                // Get available formats for the camera.
                var formats = UsbCamera.GetVideoFormat(cameraIndex);
                // Select zeroth format.
                var format = formats[0];
                Camera = new UsbCamera(cameraIndex, format);
                // Show preview on control and allow resizing.
                Camera.SetPreviewControl(CameraBox.Handle, CameraBox.ClientSize);
                CameraBox.Resize += (s, ev) => Camera.SetPreviewSize(CameraBox.ClientSize);

                Camera.Start();
            }
        }
        private void ApplicationManagerFormClosing(object sender, FormClosingEventArgs e)
        {
            NotificationLabel.Text = MotorControl.DisposeDynamixelMotors();
            if(Camera != null)
                Camera.Release();
            Refresh();
            Counter.Stop();
            Counter.Dispose();
            Thread.Sleep(2000);
        }
        #endregion

    }
}
