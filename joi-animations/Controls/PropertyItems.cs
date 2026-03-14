namespace DynamixelWizard.Controls
{
    public class PropertyItems
    {
        public PropertyItems(DirectShow.IBaseFilter vcap_source)
        {
            // Pan, Tilt, Roll, Zoom, Exposure, Iris, Focus
            CameraControl = Enum.GetValues(typeof(DirectShow.CameraControlProperty)).Cast<DirectShow.CameraControlProperty>()
                .Select(item =>
                {
                    PropertyItems.Property prop = null;
                    try
                    {
                        var cam_ctrl = vcap_source as DirectShow.IAMCameraControl;
                        if (cam_ctrl == null) throw new NotSupportedException("no IAMCameraControl Interface."); // will catched.
                        int min = 0, max = 0, step = 0, def = 0, flags = 0;
                        cam_ctrl.GetRange(item, ref min, ref max, ref step, ref def, ref flags); // COMException if not supports.

                        Action<DirectShow.CameraControlFlags, int> set = (flag, value) => cam_ctrl.Set(item, value, (int)flag);
                        Func<int> get = () => { int value = 0; cam_ctrl.Get(item, ref value, ref flags); return value; };
                        prop = new Property(min, max, step, def, flags, set, get);
                    }
                    catch (Exception) { prop = new Property(); } // available = false
                    return new { Key = item, Value = prop };
                }).ToDictionary(x => x.Key, x => x.Value);

            // Brightness, Contrast, Hue, Saturation, Sharpness, Gamma, ColorEnable, WhiteBalance, BacklightCompensation, Gain
            VideoProcAmp = Enum.GetValues(typeof(DirectShow.VideoProcAmpProperty)).Cast<DirectShow.VideoProcAmpProperty>()
                .Select(item =>
                {
                    PropertyItems.Property prop = null;
                    try
                    {
                        var vid_ctrl = vcap_source as DirectShow.IAMVideoProcAmp;
                        if (vid_ctrl == null) throw new NotSupportedException("no IAMVideoProcAmp Interface."); // will catched.
                        int min = 0, max = 0, step = 0, def = 0, flags = 0;
                        vid_ctrl.GetRange(item, ref min, ref max, ref step, ref def, ref flags); // COMException if not supports.

                        Action<DirectShow.CameraControlFlags, int> set = (flag, value) => vid_ctrl.Set(item, value, (int)flag);
                        Func<int> get = () => { int value = 0; vid_ctrl.Get(item, ref value, ref flags); return value; };
                        prop = new Property(min, max, step, def, flags, set, get);
                    }
                    catch (Exception) { prop = new Property(); } // available = false
                    return new { Key = item, Value = prop };
                }).ToDictionary(x => x.Key, x => x.Value);
        }

        public Dictionary<DirectShow.CameraControlProperty, Property> CameraControl;
        public Dictionary<DirectShow.VideoProcAmpProperty, Property> VideoProcAmp;

        public Property this[DirectShow.CameraControlProperty item] { get { return CameraControl[item]; } }

        public Property this[DirectShow.VideoProcAmpProperty item] { get { return VideoProcAmp[item]; } }

        public class Property
        {
            public int Min { get; private set; }
            public int Max { get; private set; }
            public int Step { get; private set; }
            public int Default { get; private set; }
            public DirectShow.CameraControlFlags Flags { get; private set; }
            public Action<DirectShow.CameraControlFlags, int> SetValue { get; private set; }
            public Func<int> GetValue { get; private set; }
            public bool Available { get; private set; }
            public bool CanAuto { get; private set; }

            public Property()
            {
                SetValue = (flag, value) => { };
                Available = false;
            }

            public Property(int min, int max, int step, int @default, int flags, Action<DirectShow.CameraControlFlags, int> set, Func<int> get)
            {
                Min = min;
                Max = max;
                Step = step;
                Default = @default;
                Flags = (DirectShow.CameraControlFlags)flags;
                CanAuto = (Flags & DirectShow.CameraControlFlags.Auto) == DirectShow.CameraControlFlags.Auto;
                SetValue = set;
                GetValue = get;
                Available = true;
            }

            public override string ToString()
            {
                return string.Format("Available={0}, Min={1}, Max={2}, Step={3}, Default={4}, Flags={5}", Available, Min, Max, Step, Default, Flags);
            }
        }
    }
}
