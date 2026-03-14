using System;
using System.Collections.Generic;
using System.Linq;

namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// Raw MPU6050 acceleration sample.
    /// </summary>
    public struct Mpu6050RawSample
    {
        public double AccelXg { get; set; }
        public double AccelYg { get; set; }
        public double AccelZg { get; set; }
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Hardware adapter interface for reading MPU6050 data.
    /// </summary>
    public interface IMpu6050Source
    {
        Mpu6050RawSample GetSample();
    }

    /// <summary>
    /// Converts one or more chest-mounted MPU6050 samples to IMU pitch/roll for balance control.
    /// </summary>
    public class Mpu6050ImuProvider : IImuProvider
    {
        readonly IList<IMpu6050Source> _sources;
        bool _hasFilterState;
        double _lastPitch;
        double _lastRoll;

        /// <summary>
        /// When true, swaps calculated pitch and roll to match physical sensor mounting.
        /// </summary>
        public bool SwapPitchRoll { get; set; }

        /// <summary>
        /// Use 1.0 or -1.0 to match sensor sign convention to robot convention.
        /// </summary>
        public double PitchSign { get; set; }

        /// <summary>
        /// Use 1.0 or -1.0 to match sensor sign convention to robot convention.
        /// </summary>
        public double RollSign { get; set; }

        /// <summary>
        /// Low-pass coefficient in [0..1], where higher gives faster response.
        /// </summary>
        public double FilterAlpha { get; set; }

        public Mpu6050ImuProvider(IMpu6050Source source)
            : this(new[] { source })
        {
        }

        public Mpu6050ImuProvider(IEnumerable<IMpu6050Source> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            _sources = sources.Where(s => s != null).ToList();
            if (_sources.Count == 0)
                throw new ArgumentException("At least one MPU6050 source is required.", nameof(sources));

            PitchSign = 1.0;
            RollSign = 1.0;
            FilterAlpha = 0.4;
        }

        public ImuSample GetSample()
        {
            double pitchSum = 0;
            double rollSum = 0;
            int count = 0;

            foreach (var source in _sources)
            {
                Mpu6050RawSample raw = source.GetSample();
                if (!raw.IsValid)
                    continue;

                // Accelerometer-only tilt estimate for chest orientation.
                double pitch = Math.Atan2(-raw.AccelXg, Math.Sqrt(raw.AccelYg * raw.AccelYg + raw.AccelZg * raw.AccelZg)) * (180.0 / Math.PI);
                double roll = Math.Atan2(raw.AccelYg, raw.AccelZg) * (180.0 / Math.PI);

                if (SwapPitchRoll)
                {
                    double tmp = pitch;
                    pitch = roll;
                    roll = tmp;
                }

                pitch *= PitchSign;
                roll *= RollSign;

                pitchSum += pitch;
                rollSum += roll;
                count++;
            }

            if (count == 0)
                return new ImuSample { IsValid = false };

            double meanPitch = pitchSum / count;
            double meanRoll = rollSum / count;

            if (_hasFilterState)
            {
                meanPitch = (_lastPitch * (1.0 - FilterAlpha)) + (meanPitch * FilterAlpha);
                meanRoll = (_lastRoll * (1.0 - FilterAlpha)) + (meanRoll * FilterAlpha);
            }

            _lastPitch = meanPitch;
            _lastRoll = meanRoll;
            _hasFilterState = true;

            return new ImuSample
            {
                PitchDegrees = meanPitch,
                RollDegrees = meanRoll,
                YawDegrees = 0,
                IsValid = true
            };
        }
    }
}
