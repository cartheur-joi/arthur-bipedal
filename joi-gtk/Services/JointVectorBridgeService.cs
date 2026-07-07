using Cartheur.Animals.Robot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace joi_gtk.Services;

public sealed class JointVectorBridgeService : IDisposable
{
    readonly RobotControlService _robot;
    readonly OpenRJointVectorSubject _subject = new();
    readonly BridgeObserver _observer;
    readonly object _gate = new();
    CancellationTokenSource _cts;
    Task _runTask;
    JointVectorTcpServer _server;
    bool _disposed;
    string _status = "Stopped";

    public JointVectorBridgeService(RobotControlService robot)
    {
        _robot = robot ?? throw new ArgumentNullException(nameof(robot));
        _observer = new BridgeObserver("GtkRobotJointVectorBridge", _subject, _robot);
        _observer.Applied += frame => FrameApplied?.Invoke(frame);
    }

    public event Action<string> StatusChanged;
    public event Action<JointVectorFrame> FrameApplied;
    public event Action<Exception> Faulted;

    public bool IsRunning
    {
        get
        {
            lock (_gate)
                return _runTask != null && !_runTask.IsCompleted;
        }
    }

    public string Status
    {
        get
        {
            lock (_gate)
                return _status;
        }
    }

    public string Start(string bindAddressText, int port)
    {
        ThrowIfDisposed();

        IPAddress bindAddress = ResolveBindAddress(bindAddressText);
        lock (_gate)
        {
            if (_runTask != null && !_runTask.IsCompleted)
                return $"Joint vector bridge already running on {bindAddress}:{port}.";

            if (!_robot.IsInitialized)
                _robot.Initialize();

            _observer.Start();
            _cts = new CancellationTokenSource();
            _server = new JointVectorTcpServer(_subject, port, bindAddress);
            _server.Start();
            _status = $"Listening on {bindAddress}:{port}";
            RaiseStatusChanged(_status);

            CancellationToken token = _cts.Token;
            _runTask = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        RaiseStatusChanged($"Waiting for client on {bindAddress}:{port}");
                        await _server.RunSingleClientAsync(token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    lock (_gate)
                        _status = "Faulted";
                    RaiseStatusChanged("Faulted");
                    Faulted?.Invoke(ex);
                }
                finally
                {
                    lock (_gate)
                    {
                        _server?.Dispose();
                        _server = null;
                        _cts?.Dispose();
                        _cts = null;
                        _runTask = null;
                        if (_status != "Faulted")
                            _status = "Stopped";
                    }

                    _observer.Stop();
                    if (_status != "Faulted")
                        RaiseStatusChanged("Stopped");
                }
            }, token);

            return _status;
        }
    }

    public string Stop()
    {
        ThrowIfDisposed();

        lock (_gate)
        {
            if (_runTask == null || _runTask.IsCompleted)
            {
                _status = "Stopped";
                return _status;
            }

            _status = "Stopping";
            RaiseStatusChanged(_status);
            _cts?.Cancel();
            _server?.Stop();
            return _status;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _observer.Stop();
        _server?.Dispose();
        _cts?.Dispose();
        _disposed = true;
    }

    static IPAddress ResolveBindAddress(string bindAddressText)
    {
        if (string.IsNullOrWhiteSpace(bindAddressText))
            return IPAddress.Loopback;

        string normalized = bindAddressText.Trim();
        if (string.Equals(normalized, "any", StringComparison.OrdinalIgnoreCase) ||
            normalized == "0.0.0.0")
            return IPAddress.Any;
        if (string.Equals(normalized, "loopback", StringComparison.OrdinalIgnoreCase) ||
            normalized == "127.0.0.1")
            return IPAddress.Loopback;

        if (IPAddress.TryParse(normalized, out IPAddress parsed))
            return parsed;

        throw new InvalidOperationException("Invalid bind address. Use 127.0.0.1, 0.0.0.0, any, or a concrete IP address.");
    }

    void RaiseStatusChanged(string status)
    {
        StatusChanged?.Invoke(status);
    }

    void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(JointVectorBridgeService));
    }

    sealed class BridgeObserver : IJointVectorObserver
    {
        readonly OpenRJointVectorSubject _subject;
        readonly RobotControlService _robot;
        bool _started;

        public BridgeObserver(string observerName, OpenRJointVectorSubject subject, RobotControlService robot)
        {
            ObserverName = observerName ?? throw new ArgumentNullException(nameof(observerName));
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            _robot = robot ?? throw new ArgumentNullException(nameof(robot));
        }

        public string ObserverName { get; }

        public event Action<JointVectorFrame> Applied;

        public void Start()
        {
            if (_started)
                return;

            _subject.Register(this);
            _subject.AssertReady(ObserverName);
            _started = true;
        }

        public void Stop()
        {
            if (!_started)
                return;

            _subject.DeassertReady(ObserverName);
            _started = false;
        }

        public void OnJointVector(JointVectorFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            string[] motors = frame.JointTargets.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            _robot.SetTorqueOn(motors);
            _robot.MoveToPositions(
                new Dictionary<string, int>(frame.JointTargets, StringComparer.Ordinal),
                frame.DurationMilliseconds,
                frame.InterpolationSteps);

            Applied?.Invoke(frame);
            _subject.AssertReady(ObserverName);
        }
    }
}
