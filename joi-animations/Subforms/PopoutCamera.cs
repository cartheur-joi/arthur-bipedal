using DynamixelWizard.Controls;

namespace DynamixelWizard.SubForms
{
    public partial class PopoutCamera : Form
    {
        public UsbCamera Camera { get; set; }
        public static bool Instance { get; set; }
        public static bool IsCameraLocked { get; set; }
        public PopoutCamera()
        {
            InitializeComponent();
            InitializeCamera();
        }
        void InitializeCamera()
        {
            var devices = UsbCamera.FindDevices();
            if (devices.Length == 0) return;
            var cameraIndex = 0;
            // Ger available formats for the camera.
            var formats = UsbCamera.GetVideoFormat(cameraIndex);
            // Select zeroth format.
            var format = formats[0];
            Camera = new UsbCamera(cameraIndex, format);
            // Show preview on control and allow resizing.
            Camera.SetPreviewControl(CameraBox.Handle, CameraBox.ClientSize);
            CameraBox.Resize += (s, ev) => Camera.SetPreviewSize(CameraBox.ClientSize);

            Camera.Start();
            IsCameraLocked = true;
            Instance = true;
        }

        private void CloseButtonClick(object sender, System.EventArgs e)
        {
            if(Camera != null)
            {
                Camera.Release();
                IsCameraLocked = false;
            }
            Instance = false;
            Close();
        }
    }
}
