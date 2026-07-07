using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// Represents one joint-vector payload analogous to an OPEN-R NotifyEvent body.
    /// </summary>
    public sealed class JointVectorFrame
    {
        public JointVectorFrame(
            IReadOnlyDictionary<string, int> jointTargets,
            int durationMilliseconds,
            int interpolationSteps = 8,
            long sequenceNumber = 0,
            DateTime? createdUtc = null)
        {
            if (jointTargets == null)
                throw new ArgumentNullException(nameof(jointTargets));
            if (jointTargets.Count == 0)
                throw new ArgumentException("Joint vector frame requires at least one joint.", nameof(jointTargets));

            JointTargets = new Dictionary<string, int>(jointTargets, StringComparer.Ordinal);
            DurationMilliseconds = Math.Max(1, durationMilliseconds);
            InterpolationSteps = Math.Max(1, interpolationSteps);
            SequenceNumber = sequenceNumber;
            CreatedUtc = createdUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        }

        public IReadOnlyDictionary<string, int> JointTargets { get; }
        public int DurationMilliseconds { get; }
        public int InterpolationSteps { get; }
        public long SequenceNumber { get; }
        public DateTime CreatedUtc { get; }
    }

    /// <summary>
    /// Binary serializer for portable joint-vector recording and transport.
    /// </summary>
    public static class JointVectorFrameSerializer
    {
        const uint Magic = 0x4A564631; // "JVF1"
        const ushort Version = 1;

        public static byte[] Serialize(JointVectorFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(frame.SequenceNumber);
            writer.Write(frame.CreatedUtc.ToBinary());
            writer.Write(frame.DurationMilliseconds);
            writer.Write(frame.InterpolationSteps);
            writer.Write(frame.JointTargets.Count);

            foreach ((string jointName, int positionValue) in frame.JointTargets.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(jointName);
                if (nameBytes.Length > ushort.MaxValue)
                    throw new InvalidOperationException("Joint name is too long to serialize: " + jointName);

                writer.Write((ushort)nameBytes.Length);
                writer.Write(nameBytes);
                writer.Write(positionValue);
            }

            writer.Flush();
            return stream.ToArray();
        }

        public static JointVectorFrame Deserialize(byte[] payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            using var stream = new MemoryStream(payload, writable: false);
            return Deserialize(stream);
        }

        public static JointVectorFrame Deserialize(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            uint magic = reader.ReadUInt32();
            if (magic != Magic)
                throw new InvalidDataException("Payload is not a JointVectorFrame message.");

            ushort version = reader.ReadUInt16();
            if (version != Version)
                throw new InvalidDataException("Unsupported JointVectorFrame version: " + version);

            long sequenceNumber = reader.ReadInt64();
            DateTime createdUtc = DateTime.FromBinary(reader.ReadInt64());
            int durationMilliseconds = reader.ReadInt32();
            int interpolationSteps = reader.ReadInt32();
            int jointCount = reader.ReadInt32();

            if (jointCount <= 0)
                throw new InvalidDataException("JointVectorFrame must contain at least one joint.");

            var targets = new Dictionary<string, int>(jointCount, StringComparer.Ordinal);
            for (int i = 0; i < jointCount; i++)
            {
                ushort nameLength = reader.ReadUInt16();
                string jointName = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                int positionValue = reader.ReadInt32();
                targets[jointName] = positionValue;
            }

            JointVectorFrame frame = new JointVectorFrame(
                targets,
                durationMilliseconds,
                interpolationSteps,
                sequenceNumber,
                createdUtc);

            return frame;
        }
    }

    public enum JointVectorReplayTimingMode
    {
        FrameDuration = 0,
        RecordedGap = 1
    }

    /// <summary>
    /// Appends joint-vector frames to a length-prefixed capture log.
    /// </summary>
    public sealed class JointVectorFrameRecorder : IDisposable
    {
        readonly FileStream _stream;
        readonly BinaryWriter _writer;
        bool _disposed;

        public JointVectorFrameRecorder(string path, bool append = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            _stream = new FileStream(
                path,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.Read);
            _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
        }

        public void Record(JointVectorFrame frame)
        {
            ThrowIfDisposed();

            byte[] payload = JointVectorFrameSerializer.Serialize(frame);
            _writer.Write(payload.Length);
            _writer.Write(payload);
            _writer.Flush();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _writer.Dispose();
            _stream.Dispose();
            _disposed = true;
        }

        void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JointVectorFrameRecorder));
        }
    }

    /// <summary>
    /// Reads previously captured joint-vector frames from a length-prefixed log.
    /// </summary>
    public sealed class JointVectorFrameLogReader : IDisposable
    {
        readonly FileStream _stream;
        readonly BinaryReader _reader;
        bool _disposed;

        public JointVectorFrameLogReader(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
        }

        public bool TryReadNext(out JointVectorFrame frame)
        {
            ThrowIfDisposed();

            frame = null;
            if (_stream.Position >= _stream.Length)
                return false;

            int payloadLength = _reader.ReadInt32();
            if (payloadLength <= 0)
                throw new InvalidDataException("Encountered invalid joint-vector payload length.");

            byte[] payload = _reader.ReadBytes(payloadLength);
            if (payload.Length != payloadLength)
                throw new EndOfStreamException("Joint-vector payload was truncated.");

            frame = JointVectorFrameSerializer.Deserialize(payload);
            return true;
        }

        public IEnumerable<JointVectorFrame> ReadAll()
        {
            while (TryReadNext(out JointVectorFrame frame))
                yield return frame;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _reader.Dispose();
            _stream.Dispose();
            _disposed = true;
        }

        void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JointVectorFrameLogReader));
        }
    }

    /// <summary>
    /// Replays captured joint-vector logs back through the OPEN-R style subject.
    /// </summary>
    public sealed class JointVectorReplayPlayer
    {
        readonly OpenRJointVectorSubject _subject;

        public JointVectorReplayPlayer(OpenRJointVectorSubject subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        }

        public async Task ReplayAsync(
            IEnumerable<JointVectorFrame> frames,
            JointVectorReplayTimingMode timingMode = JointVectorReplayTimingMode.FrameDuration,
            CancellationToken cancellationToken = default)
        {
            if (frames == null)
                throw new ArgumentNullException(nameof(frames));

            JointVectorFrame previous = null;

            foreach (JointVectorFrame frame in frames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _subject.Publish(frame);

                int delayMilliseconds = ResolveDelayMilliseconds(previous, frame, timingMode);
                previous = frame;

                if (delayMilliseconds > 0)
                    await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task ReplayFileAsync(
            string path,
            JointVectorReplayTimingMode timingMode = JointVectorReplayTimingMode.FrameDuration,
            CancellationToken cancellationToken = default)
        {
            using var reader = new JointVectorFrameLogReader(path);
            return ReplayAsync(reader.ReadAll(), timingMode, cancellationToken);
        }

        static int ResolveDelayMilliseconds(
            JointVectorFrame previous,
            JointVectorFrame current,
            JointVectorReplayTimingMode timingMode)
        {
            if (current == null)
                return 0;

            if (timingMode == JointVectorReplayTimingMode.RecordedGap && previous != null)
            {
                double gap = (current.CreatedUtc - previous.CreatedUtc).TotalMilliseconds;
                return Math.Max(0, (int)Math.Round(gap));
            }

            return Math.Max(0, current.DurationMilliseconds);
        }
    }

    /// <summary>
    /// Sends length-prefixed joint-vector payloads over a TCP connection.
    /// </summary>
    public sealed class JointVectorTcpClient : IDisposable
    {
        readonly TcpClient _client;
        readonly NetworkStream _stream;
        bool _disposed;

        public JointVectorTcpClient(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host is required.", nameof(host));

            _client = new TcpClient();
            _client.Connect(host, port);
            _stream = _client.GetStream();
        }

        public void Send(JointVectorFrame frame)
        {
            ThrowIfDisposed();

            byte[] payload = JointVectorFrameSerializer.Serialize(frame);
            byte[] prefix = BitConverter.GetBytes(payload.Length);
            _stream.Write(prefix, 0, prefix.Length);
            _stream.Write(payload, 0, payload.Length);
            _stream.Flush();
        }

        public async Task SendAsync(JointVectorFrame frame, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            byte[] payload = JointVectorFrameSerializer.Serialize(frame);
            byte[] prefix = BitConverter.GetBytes(payload.Length);
            await _stream.WriteAsync(prefix, cancellationToken).ConfigureAwait(false);
            await _stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _stream.Dispose();
            _client.Dispose();
            _disposed = true;
        }

        void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JointVectorTcpClient));
        }
    }

    /// <summary>
    /// Receives length-prefixed joint-vector payloads from one TCP client and publishes them.
    /// </summary>
    public sealed class JointVectorTcpServer : IDisposable
    {
        readonly OpenRJointVectorSubject _subject;
        readonly TcpListener _listener;
        bool _disposed;

        public JointVectorTcpServer(OpenRJointVectorSubject subject, int port, IPAddress ipAddress = null)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            _listener = new TcpListener(ipAddress ?? IPAddress.Loopback, port);
        }

        public void Start()
        {
            ThrowIfDisposed();
            _listener.Start();
        }

        public void Stop()
        {
            if (_disposed)
                return;

            _listener.Stop();
        }

        public async Task RunSingleClientAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            using NetworkStream stream = client.GetStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] prefix = await ReadExactAsync(stream, sizeof(int), cancellationToken).ConfigureAwait(false);
                if (prefix == null)
                    break;

                int payloadLength = BitConverter.ToInt32(prefix, 0);
                if (payloadLength <= 0)
                    throw new InvalidDataException("Encountered invalid TCP joint-vector payload length.");

                byte[] payload = await ReadExactAsync(stream, payloadLength, cancellationToken).ConfigureAwait(false);
                if (payload == null)
                    throw new EndOfStreamException("TCP joint-vector payload was truncated.");

                JointVectorFrame frame = JointVectorFrameSerializer.Deserialize(payload);
                _subject.Publish(frame);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _listener.Stop();
            _disposed = true;
        }

        static async Task<byte[]> ReadExactAsync(
            NetworkStream stream,
            int length,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int bytesRead = await stream.ReadAsync(
                    buffer.AsMemory(offset, length - offset),
                    cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    if (offset == 0)
                        return null;

                    return null;
                }

                offset += bytesRead;
            }

            return buffer;
        }

        void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JointVectorTcpServer));
        }
    }

    public enum OpenRReadyState
    {
        Unknown = 0,
        AssertedReady = 1,
        DeassertedReady = 2
    }

    /// <summary>
    /// Receives joint-vector payloads and is responsible for re-asserting readiness.
    /// </summary>
    public interface IJointVectorObserver
    {
        string ObserverName { get; }
        void OnJointVector(JointVectorFrame frame);
    }

    /// <summary>
    /// Host-side approximation of OPEN-R subject/observer delivery semantics for joint vectors.
    /// </summary>
    public sealed class OpenRJointVectorSubject
    {
        readonly object _sync = new object();
        readonly Dictionary<string, ObserverLink> _links = new Dictionary<string, ObserverLink>(StringComparer.Ordinal);

        public void Register(IJointVectorObserver observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            lock (_sync)
            {
                _links[observer.ObserverName] = new ObserverLink(observer);
            }
        }

        public void DeassertReady(string observerName)
        {
            if (string.IsNullOrWhiteSpace(observerName))
                throw new ArgumentException("Observer name is required.", nameof(observerName));

            lock (_sync)
            {
                ObserverLink link = GetLink(observerName);
                link.State = OpenRReadyState.DeassertedReady;
                link.PendingFrame = null;
            }
        }

        public void AssertReady(string observerName)
        {
            if (string.IsNullOrWhiteSpace(observerName))
                throw new ArgumentException("Observer name is required.", nameof(observerName));

            ObserverLink link;
            JointVectorFrame pendingFrame;

            lock (_sync)
            {
                link = GetLink(observerName);
                link.State = OpenRReadyState.AssertedReady;
                pendingFrame = link.PendingFrame;
                if (pendingFrame != null)
                {
                    link.PendingFrame = null;
                    // Model the single-transfer handshake: after one delivery, the
                    // observer must explicitly assert readiness again.
                    link.State = OpenRReadyState.Unknown;
                }
            }

            if (pendingFrame != null)
                link.Observer.OnJointVector(pendingFrame);
        }

        public void Publish(JointVectorFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            List<Delivery> deliveries = new List<Delivery>();

            lock (_sync)
            {
                foreach (ObserverLink link in _links.Values)
                {
                    switch (link.State)
                    {
                        case OpenRReadyState.AssertedReady:
                            link.State = OpenRReadyState.Unknown;
                            deliveries.Add(new Delivery(link.Observer, frame));
                            break;

                        case OpenRReadyState.Unknown:
                            link.PendingFrame = frame;
                            break;

                        case OpenRReadyState.DeassertedReady:
                            link.PendingFrame = null;
                            break;
                    }
                }
            }

            foreach (Delivery delivery in deliveries)
                delivery.Observer.OnJointVector(delivery.Frame);
        }

        public IReadOnlyDictionary<string, OpenRReadyState> SnapshotStates()
        {
            lock (_sync)
            {
                return _links.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.State,
                    StringComparer.Ordinal);
            }
        }

        ObserverLink GetLink(string observerName)
        {
            if (!_links.TryGetValue(observerName, out ObserverLink link))
                throw new InvalidOperationException("Observer is not registered: " + observerName);

            return link;
        }

        sealed class ObserverLink
        {
            public ObserverLink(IJointVectorObserver observer)
            {
                Observer = observer;
                State = OpenRReadyState.Unknown;
            }

            public IJointVectorObserver Observer { get; }
            public OpenRReadyState State { get; set; }
            public JointVectorFrame PendingFrame { get; set; }
        }

        readonly struct Delivery
        {
            public Delivery(IJointVectorObserver observer, JointVectorFrame frame)
            {
                Observer = observer;
                Frame = frame;
            }

            public IJointVectorObserver Observer { get; }
            public JointVectorFrame Frame { get; }
        }
    }

    /// <summary>
    /// Adapts incoming joint vectors onto the existing trajectory player.
    /// </summary>
    public sealed class OpenRJointVectorPlayer : IJointVectorObserver
    {
        readonly OpenRJointVectorSubject _subject;

        public OpenRJointVectorPlayer(
            string observerName,
            OpenRJointVectorSubject subject,
            MotionTrajectoryPlayer trajectoryPlayer)
        {
            if (string.IsNullOrWhiteSpace(observerName))
                throw new ArgumentException("Observer name is required.", nameof(observerName));

            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            TrajectoryPlayer = trajectoryPlayer ?? throw new ArgumentNullException(nameof(trajectoryPlayer));
            ObserverName = observerName;
        }

        public string ObserverName { get; }
        public MotionTrajectoryPlayer TrajectoryPlayer { get; }

        public void Start()
        {
            _subject.Register(this);
            _subject.AssertReady(ObserverName);
        }

        public void Stop()
        {
            _subject.DeassertReady(ObserverName);
        }

        public void OnJointVector(JointVectorFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            var step = new MotionTrajectoryStep(
                new Dictionary<string, int>(frame.JointTargets, StringComparer.Ordinal),
                frame.DurationMilliseconds,
                frame.InterpolationSteps);

            TrajectoryPlayer.ExecuteStep(step);
            _subject.AssertReady(ObserverName);
        }
    }
}
