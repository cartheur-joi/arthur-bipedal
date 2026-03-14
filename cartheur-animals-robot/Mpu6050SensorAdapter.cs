using System;

namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// Adapts Cartheur.Devices AccelerometerService to IMpu6050Source.
    /// </summary>
    public class Mpu6050SensorAdapter : IMpu6050Source, IDisposable
    {
        readonly AccelerometerService _service;
        readonly object _sampleLock = new object();
        Mpu6050RawSample _latestSample;
        volatile bool _hasSample;

        public Mpu6050SensorAdapter(int scanRateMilliseconds = 20, int busId = 1, int address = 0x68, bool autoStart = true)
        {
            _service = new AccelerometerService(scanRateMilliseconds, busId, address);
            _service.MeasurementTaken += OnMeasurementTaken;

            if (autoStart)
                _service.Start();
        }

        public Mpu6050RawSample GetSample()
        {
            if (!_hasSample)
                return new Mpu6050RawSample { IsValid = false };

            lock (_sampleLock)
            {
                return _latestSample;
            }
        }

        public void Start()
        {
            _service.Start();
        }

        public void Stop()
        {
            _service.Stop();
        }

        void OnMeasurementTaken(object sender, MpuSensorEventArgs e)
        {
            if (e == null || e.Values == null || e.Values.Length == 0)
                return;

            MpuSensorValue value = e.Values[e.Values.Length - 1];
            lock (_sampleLock)
            {
                _latestSample = new Mpu6050RawSample
                {
                    AccelXg = value.AccelerationX,
                    AccelYg = value.AccelerationY,
                    AccelZg = value.AccelerationZ,
                    IsValid = true
                };
            }
            _hasSample = true;
        }

        public void Dispose()
        {
            _service.MeasurementTaken -= OnMeasurementTaken;
            _service.Stop();
        }
    }
}
