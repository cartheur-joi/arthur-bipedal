using System.ComponentModel;
using Timer = System.Windows.Forms.Timer;

namespace DynamixelWizard.Controls
{
    /// <summary>
    /// An enumeration to set the interval of the flash.
    /// </summary>
    [Description("Select Flasher Interval")]
    public enum FlashIntervalSpeed { Slow = 0, Mid = 1, Fast = 2, BlipSlow = 3, BlipMid = 4, BlipFast = 5, FlashFinite = 6, FlashFiniteSlow = 7 }

    [Description("Flasher Button Control")]
    public partial class FlashButton : Button
    {
        protected const int FlashIntervalMiddle = 500;
        protected const int FlashIntervalFast = 200;
        protected const int FlashIntervalSlow = 1000;
        protected const int FlashIntervalBlipOn = 70;
        protected Color ColorOff = SystemColors.Control;
        protected Color ColorOn = Color.LightGreen;

        protected bool IsFlashEnabled = false;
        protected int FlashPeriodOn;
        protected int FlashPeriodOff;
        protected int FlashingNumber;
        protected Timer FlashIntervalTimer;
        protected int FlashNumberCounter = 0;

        [Browsable(true), Category("Appearance"), 
        Description("Get/Set button color while 'OFF' flash period or disabled"),RefreshProperties(RefreshProperties.Repaint)]
        public Color FlasherButtonColorOff { get { return ColorOff; } set { ColorOff = value; } }

        [Browsable(true),Category("Appearance"),
        Description("Get/Set button color while 'ON' flash period"),RefreshProperties(RefreshProperties.Repaint)]
        public Color FlasherButtonColorOn { get { return ColorOn; } set { ColorOn = value; } }

        [Browsable(true), Category("Appearance"),
        Description("Set the number of flashes"), RefreshProperties(RefreshProperties.Repaint)]
        public int FlashNumber { get { return FlashingNumber; } set { FlashingNumber = value; } }

        [Browsable(true),Category("Appearance"),
        Description("Get flasher status, True=flashing, False=disabled"), RefreshProperties(RefreshProperties.Repaint)]
        public bool FlasherButtonStatus { get { return IsFlashEnabled; } }       // True = flashing, false = inactive.

        public FlashButton()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // TODO: Add custom paint code here
            base.OnPaint(pe);
        }

        [Browsable(true), Category("Appearance"),
        Description("Enable button flashing, select interval with standard / blip mode"), RefreshProperties(RefreshProperties.Repaint)]
        public void FlasherButtonStart(FlashIntervalSpeed selectFlashMode)
        {
            switch (selectFlashMode)
            {
                case FlashIntervalSpeed.Slow:
                    FlashPeriodOn = FlashIntervalSlow / 2;
                    FlashPeriodOff = FlashPeriodOn;
                    break;
                case FlashIntervalSpeed.Mid:
                    FlashPeriodOn = FlashIntervalMiddle / 2;
                    FlashPeriodOff = FlashPeriodOn;
                    break;
                case FlashIntervalSpeed.Fast:
                    FlashPeriodOn = FlashIntervalFast / 2;
                    FlashPeriodOff = FlashPeriodOn;
                    break;
                case FlashIntervalSpeed.BlipSlow:
                    FlashPeriodOn = FlashIntervalBlipOn;
                    FlashPeriodOff = FlashIntervalSlow - FlashIntervalBlipOn;
                    break;
                case FlashIntervalSpeed.BlipMid:
                    FlashPeriodOn = FlashIntervalBlipOn;
                    FlashPeriodOff = FlashIntervalMiddle - FlashIntervalBlipOn;
                    break;
                case FlashIntervalSpeed.BlipFast:
                    FlashPeriodOn = FlashIntervalBlipOn;
                    FlashPeriodOff = FlashIntervalFast - FlashIntervalBlipOn;
                    break;
                case FlashIntervalSpeed.FlashFinite:
                    FlashPeriodOn = FlashIntervalFast / 2;
                    FlashPeriodOff = FlashPeriodOn;
                    break;
                case FlashIntervalSpeed.FlashFiniteSlow:
                    FlashPeriodOn = FlashIntervalSlow / 2;
                    FlashPeriodOff = FlashPeriodOn;
                    break;
                default:
                    return;
            }
            if (IsFlashEnabled == false)
            {
                IsFlashEnabled = true;
                FlashIntervalTimer = new Timer
                {
                    Interval = FlashPeriodOn
                };
                base.BackColor = ColorOn;
                
                if (selectFlashMode == FlashIntervalSpeed.FlashFinite || selectFlashMode == FlashIntervalSpeed.FlashFiniteSlow)
                {
                    FlashIntervalTimer.Tick += FlashIntervalFiniteOnTick;
                }
                else
                {
                    FlashIntervalTimer.Tick += FlashIntervalTimerOnTick;
                }
                FlashIntervalTimer.Start();
            }
        }
        [Description("Disable button flashing")]
        [Category("Layout")]
        [Browsable(true)]
        public void FlasherButtonStop()
        {
            if (FlashIntervalTimer != null)
            {
                base.BackColor = ColorOff;
                FlashIntervalTimer.Stop();
                FlashIntervalTimer.Dispose();
            }
            IsFlashEnabled = false;
        }

        protected void FlashIntervalFiniteOnTick(object obj, EventArgs e)
        {
            if (base.BackColor == ColorOff)
            {
                base.BackColor = ColorOn;
                FlashIntervalTimer.Interval = FlashPeriodOn;
                FlashNumberCounter++;
                if (FlashNumberCounter == FlashNumber)
                {
                    FlasherButtonStop();
                    FlashNumberCounter = 0;
                }
            }
            else
            {
                base.BackColor = ColorOff;
                FlashIntervalTimer.Interval = FlashPeriodOff;
            }
            Invalidate();
        }

        protected void FlashIntervalTimerOnTick(object obj, EventArgs e)
        {
            if (base.BackColor == ColorOff)
            {
                base.BackColor = ColorOn;
                FlashIntervalTimer.Interval = FlashPeriodOn;
            }
            else
            {
                base.BackColor = ColorOff;
                FlashIntervalTimer.Interval = FlashPeriodOff;
            }
            Invalidate();
        }

        [Browsable(true), Category("Appearance"),
        Description("Set Flasher Color, ON and OFF"), RefreshProperties(RefreshProperties.Repaint)]
        public void FlasherButtonColor(Color colorOn, Color colorOff)
        {
            ColorOn = colorOn;
            ColorOff = colorOff;
        }
    }
}
