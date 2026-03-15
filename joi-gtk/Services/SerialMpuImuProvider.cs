using Cartheur.Animals.Robot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Text;

namespace joi_gtk.Services;

public sealed class SerialMpuImuProvider : IImuProvider, IDisposable
{
    readonly object _gate = new();
    readonly string _portName;
    readonly int _baudRate;
    readonly int _staleMilliseconds;
    readonly StringBuilder _buffer = new();
    SerialPort _port;
    ImuSample _latestSample;
    DateTime _latestUtc = DateTime.MinValue;
    string _lastError = string.Empty;

    public SerialMpuImuProvider(string portName, int baudRate, int staleMilliseconds = 1000)
    {
        _portName = portName;
        _baudRate = baudRate;
        _staleMilliseconds = Math.Max(200, staleMilliseconds);
    }

    public string Status
    {
        get
        {
            lock (_gate)
            {
                bool connected = _port != null && _port.IsOpen;
                string freshness = _latestUtc == DateTime.MinValue
                    ? "no-sample"
                    : $"{Math.Max(0, (int)(DateTime.UtcNow - _latestUtc).TotalMilliseconds)}ms";
                string error = string.IsNullOrWhiteSpace(_lastError) ? "none" : _lastError;
                return $"port={_portName} baud={_baudRate} connected={connected} age={freshness} error={error}";
            }
        }
    }

    public ImuSample GetSample()
    {
        lock (_gate)
        {
            EnsurePortOpen();
            if (_port == null || !_port.IsOpen)
                return new ImuSample { IsValid = false };

            try
            {
                DrainIncomingAndParse();
            }
            catch (TimeoutException)
            {
                // Keep last sample.
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _latestSample = new ImuSample { IsValid = false };
            }

            bool stale = _latestUtc == DateTime.MinValue || (DateTime.UtcNow - _latestUtc).TotalMilliseconds > _staleMilliseconds;
            if (stale)
                return new ImuSample { IsValid = false };

            return _latestSample;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_port == null)
                return;
            try { _port.Close(); } catch { }
            _port.Dispose();
            _port = null;
        }
    }

    void EnsurePortOpen()
    {
        if (_port != null && _port.IsOpen)
            return;

        try
        {
            _port?.Dispose();
            _port = new SerialPort(_portName, _baudRate)
            {
                NewLine = "\r\n",
                ReadTimeout = 900,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DtrEnable = true,
                RtsEnable = true
            };
            _port.Open();
            _port.DiscardInBuffer();
            _buffer.Clear();
            _lastError = string.Empty;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            _port = null;
        }
    }

    static bool TryParseSample(string line, out ImuSample sample)
    {
        sample = new ImuSample { IsValid = false };
        if (string.IsNullOrWhiteSpace(line))
            return false;

        Dictionary<string, double> values = new(StringComparer.OrdinalIgnoreCase);
        string[] tokens = line.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string[] kv = token.Trim().Split('=', 2);
            if (kv.Length != 2)
                continue;
            if (!double.TryParse(kv[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                continue;
            values[kv[0].Trim()] = value;
        }

        if (values.Count == 0)
            return false;

        // Prefer chest values from Arduino format: Xc/Yc/Zc.
        bool hasPitch = values.TryGetValue("Xc", out double pitch) || values.TryGetValue("Xs", out pitch);
        bool hasRoll = values.TryGetValue("Yc", out double roll) || values.TryGetValue("Ys", out roll);
        bool hasYaw = values.TryGetValue("Zc", out double yaw) || values.TryGetValue("Zs", out yaw);
        if (!hasPitch || !hasRoll)
            return false;

        sample = new ImuSample
        {
            PitchDegrees = pitch,
            RollDegrees = roll,
            YawDegrees = hasYaw ? yaw : 0.0,
            IsValid = true
        };
        return true;
    }

    void DrainIncomingAndParse()
    {
        if (_port == null || !_port.IsOpen)
            return;

        string incoming = _port.ReadExisting();
        if (string.IsNullOrEmpty(incoming))
        {
            try
            {
                incoming = _port.ReadLine() + "\r\n";
            }
            catch (TimeoutException)
            {
                incoming = string.Empty;
            }
        }
        if (string.IsNullOrEmpty(incoming))
            return;

        _buffer.Append(incoming);
        while (true)
        {
            int newlineIndex = -1;
            for (int i = 0; i < _buffer.Length; i++)
            {
                char c = _buffer[i];
                if (c == '\n' || c == '\r')
                {
                    newlineIndex = i;
                    break;
                }
            }

            if (newlineIndex < 0)
                break;

            string line = _buffer.ToString(0, newlineIndex);

            int removeCount = newlineIndex + 1;
            while (removeCount < _buffer.Length && (_buffer[removeCount] == '\n' || _buffer[removeCount] == '\r'))
                removeCount++;
            _buffer.Remove(0, removeCount);

            if (TryParseSample(line, out ImuSample sample))
            {
                _latestSample = sample;
                _latestUtc = DateTime.UtcNow;
                _lastError = string.Empty;
            }
        }
    }
}
