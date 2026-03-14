using System;

namespace Cartheur.Animals.Robot
{
    public class MpuSensorEventArgs : EventArgs
    {
        public byte Status { get; set; }
        public float SamplePeriod { get; set; }
        public MpuSensorValue [] Values { get; set; }
    }
}
