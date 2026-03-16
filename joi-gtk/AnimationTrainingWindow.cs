using Gtk;
using joi_gtk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace joi_gtk;

public sealed class AnimationTrainingWindow : Window
{
    readonly RobotControlService _robot;
    readonly AnimationTrainingService _training;
    readonly PocketSphinxVoiceCommandSource _voice = new();
    readonly StringBuilder _log = new();

    readonly ComboBoxText _armSelection = new();
    readonly Entry _replayPhraseEntry = new() { Text = "seated_handshake" };
    readonly Entry _stepIntervalEntry = new() { Text = "1000", WidthChars = 7 };
    readonly Label _statusLabel = new("Idle");
    readonly Label _voiceStatusLabel = new("Voice: off");
    readonly Label _safetyStatusLabel = new("Safety: not started");
    readonly TextView _logView = new() { Editable = false, CursorVisible = false, Monospace = true, WrapMode = WrapMode.WordChar };
    readonly Button _pauseTrainingButton;
    uint _captureTimerId;
    uint _safetyTimerId;
    int _captureIntervalMs;
    bool _hasActiveSession;
    bool _isPaused;
    bool _safetyPollInProgress;
    int _lastSafetyErrorCount = -1;
    int _lastSafetyOverloadCount = -1;
    int _lastSafetyThermalCount = -1;
    int _lastSafetyVoltageCount = -1;

    public AnimationTrainingWindow(RobotControlService robot) : base("Animation Training")
    {
        GtkWindowIconService.Apply(this);
        _robot = robot;
        _training = new AnimationTrainingService(robot);
        SetDefaultSize(900, 560);
        BorderWidth = 10;
        DeleteEvent += (_, e) =>
        {
            StopCaptureTimer();
            StopSafetyPolling();
            _voice.Dispose();
            e.RetVal = false;
        };

        _voice.PhraseDetected += OnPhraseDetected;
        _voice.ListenWindowStarted += OnListenWindowStarted;
        _voice.ListenWindowEnded += OnListenWindowEnded;
        Shown += (_, _) => EnsureInitializedInBackground();

        Box root = new(Orientation.Vertical, 8);
        Add(root);

        Label title = new("Animation Training (Arms)");
        title.Xalign = 0;
        root.PackStart(title, false, false, 0);

        Grid config = new()
        {
            ColumnSpacing = 8,
            RowSpacing = 8
        };
        config.Attach(new Label("Start phrase") { Xalign = 0 }, 0, 0, 1, 1);
        config.Attach(new Label("Begin animation training") { Xalign = 0 }, 1, 0, 2, 1);

        _armSelection.AppendText("Left Arm");
        _armSelection.AppendText("Right Arm");
        _armSelection.Active = 1;
        config.Attach(new Label("Arm") { Xalign = 0 }, 0, 1, 1, 1);
        config.Attach(_armSelection, 1, 1, 1, 1);

        config.Attach(new Label("Replay phrase") { Xalign = 0 }, 0, 2, 1, 1);
        config.Attach(_replayPhraseEntry, 1, 2, 2, 1);
        config.Attach(new Label("Canonical AT-001 phrase: seated_handshake") { Xalign = 0 }, 0, 4, 3, 1);

        config.Attach(new Label("Capture ms") { Xalign = 0 }, 0, 3, 1, 1);
        config.Attach(_stepIntervalEntry, 1, 3, 1, 1);
        config.Attach(_statusLabel, 2, 3, 1, 1);
        config.Attach(_voiceStatusLabel, 2, 2, 1, 1);
        config.Attach(_safetyStatusLabel, 1, 5, 2, 1);
        root.PackStart(config, false, false, 0);

        Box actionRow = new(Orientation.Horizontal, 8);
        actionRow.PackStart(CreateButton("Begin Animation Training", (_, _) => BeginTraining()), false, false, 0);
        _pauseTrainingButton = CreateButton("Pause Training", (_, _) => TogglePauseTraining());
        _pauseTrainingButton.Sensitive = false;
        actionRow.PackStart(_pauseTrainingButton, false, false, 0);
        actionRow.PackStart(CreateButton("Stop && Save", (_, _) => StopAndSave()), false, false, 0);
        actionRow.PackStart(CreateButton("Replay Phrase", (_, _) => ReplayPhrase()), false, false, 0);
        actionRow.PackStart(CreateButton("Voice ON", (_, _) => StartVoiceListener()), false, false, 0);
        actionRow.PackStart(CreateButton("Voice OFF", (_, _) => StopVoiceListener()), false, false, 0);
        actionRow.PackStart(CreateButton("Close", (_, _) => Close()), false, false, 0);
        root.PackStart(actionRow, false, false, 0);

        ScrolledWindow logScroll = new()
        {
            HscrollbarPolicy = PolicyType.Automatic,
            VscrollbarPolicy = PolicyType.Automatic
        };
        logScroll.Add(_logView);
        root.PackStart(logScroll, true, true, 0);

        AppendLog("Ready. Use 'Begin animation training' flow to capture arm poses.");
        StartVoiceListener();
        StartSafetyPolling();
    }

    static Button CreateButton(string text, EventHandler onClick)
    {
        Button button = new(text);
        button.Clicked += onClick;
        return button;
    }

    void BeginTraining()
    {
        if (!TryGetCaptureInterval(out int intervalMs))
            return;

        StopCaptureTimer();

        try
        {
            string arm = _armSelection.ActiveText ?? "Left Arm";
            string replayPhrase = _replayPhraseEntry.Text;
            Dictionary<string, int> firstFrame = _training.BeginSession(arm, replayPhrase);
            _captureIntervalMs = intervalMs;
            _hasActiveSession = true;
            _isPaused = false;
            _pauseTrainingButton.Sensitive = true;
            _pauseTrainingButton.Label = "Pause Training";
            _statusLabel.Text = "Training: awaiting input";
            BeepSignal();
            AppendLog($"Session started. arm={arm}, phrase=\"{replayPhrase}\"");
            LogFrame("Captured", firstFrame);

            StartCaptureTimer(_captureIntervalMs);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Training: failed";
            AppendLog($"Start ERROR: {ex.Message}");
        }
    }

    void EnsureInitializedInBackground()
    {
        if (_robot.IsInitialized)
            return;

        _statusLabel.Text = "Initializing robot...";
        AppendLog("Initialization started in background for Animation Training.");

        _ = Task.Run(() =>
        {
            try
            {
                string result = _robot.Initialize();
                Application.Invoke(delegate
                {
                    _statusLabel.Text = "Idle";
                    AppendLog(result);
                    PollSafetySnapshot();
                });
            }
            catch (Exception ex)
            {
                Application.Invoke(delegate
                {
                    _statusLabel.Text = "Initialize: failed";
                    AppendLog($"Initialize ERROR: {ex.Message}");
                });
            }
        });
    }

    void StopAndSave()
    {
        StopCaptureTimer();
        try
        {
            int frameCount = _training.StopAndSaveSession();
            _hasActiveSession = false;
            _isPaused = false;
            _pauseTrainingButton.Sensitive = false;
            _pauseTrainingButton.Label = "Pause Training";
            _statusLabel.Text = "Training: saved";
            PlayTrainingCompletedTone();
            AppendLog($"Session saved ({frameCount} frames).");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Training: save failed";
            AppendLog($"Save ERROR: {ex.Message}");
        }
    }

    void TogglePauseTraining()
    {
        if (!_hasActiveSession)
        {
            AppendLog("No active training session to pause.");
            return;
        }

        if (!_isPaused)
        {
            StopCaptureTimer();
            _isPaused = true;
            _pauseTrainingButton.Label = "Resume Training";
            _statusLabel.Text = "Training: paused";
            AppendLog("Training paused.");
            return;
        }

        if (_captureIntervalMs < 200)
        {
            if (!TryGetCaptureInterval(out _captureIntervalMs))
                return;
        }

        _isPaused = false;
        _pauseTrainingButton.Label = "Pause Training";
        _statusLabel.Text = "Training: resumed";
        AppendLog("Training resumed.");
        StartCaptureTimer(_captureIntervalMs);
    }

    void ReplayPhrase()
    {
        StopCaptureTimer();
        try
        {
            int frames = _training.ReplayLatest(_replayPhraseEntry.Text);
            _statusLabel.Text = "Replay: complete";
            AppendLog($"Replay complete ({frames} frames) for phrase=\"{_replayPhraseEntry.Text}\".");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Replay: failed";
            AppendLog($"Replay ERROR: {ex.Message}");
        }
    }

    void StartVoiceListener()
    {
        string message = _voice.Start();
        _voiceStatusLabel.Text = _voice.IsRunning ? "Voice: listening" : "Voice: unavailable";
        AppendLog(message);
    }

    void StopVoiceListener()
    {
        string message = _voice.Stop();
        _voiceStatusLabel.Text = "Voice: off";
        AppendLog(message);
    }

    void StartSafetyPolling()
    {
        if (_safetyTimerId != 0)
            return;

        _safetyTimerId = GLib.Timeout.Add(1000, () =>
        {
            PollSafetySnapshot();
            return true;
        });
    }

    void StopSafetyPolling()
    {
        if (_safetyTimerId == 0)
            return;

        GLib.Source.Remove(_safetyTimerId);
        _safetyTimerId = 0;
    }

    void PollSafetySnapshot()
    {
        if (!_robot.IsInitialized || _safetyPollInProgress)
            return;

        _safetyPollInProgress = true;
        _ = Task.Run(() =>
        {
            try
            {
                IReadOnlyList<MotorMonitorReading> snapshot = _robot.ReadMotorMonitoringSnapshot(900);
                int errors = snapshot.Count(r => !r.CommunicationOk);
                int overloads = snapshot.Count(r => r.Overload);
                int thermal = snapshot.Count(r => r.ThermalViolation);
                int voltage = snapshot.Count(r => r.VoltageViolation);

                Application.Invoke(delegate
                {
                    _safetyStatusLabel.Text =
                        $"Safety: errors={errors}, overloads={overloads}, thermal={thermal}, voltage={voltage}";
                    if (errors != _lastSafetyErrorCount ||
                        overloads != _lastSafetyOverloadCount ||
                        thermal != _lastSafetyThermalCount ||
                        voltage != _lastSafetyVoltageCount)
                    {
                        AppendLog(
                            $"Safety update: errors={errors}, overloads={overloads}, thermal={thermal}, voltage={voltage}");
                    }
                    if (overloads > 0 || thermal > 0 || voltage > 0)
                    {
                        _statusLabel.Text = "Safety: ALERT";
                        BeepSignal();
                    }
                    _lastSafetyErrorCount = errors;
                    _lastSafetyOverloadCount = overloads;
                    _lastSafetyThermalCount = thermal;
                    _lastSafetyVoltageCount = voltage;
                });
            }
            catch (Exception ex)
            {
                Application.Invoke(delegate
                {
                    _safetyStatusLabel.Text = "Safety: unavailable";
                    AppendLog($"Safety monitor ERROR: {ex.Message}");
                });
            }
            finally
            {
                _safetyPollInProgress = false;
            }
        });
    }

    void OnPhraseDetected(string phrase, double confidence)
    {
        Application.Invoke(delegate
        {
            string normalized = phrase.Trim().ToLowerInvariant();
            if (normalized.Length == 0)
                return;

            string confidenceText = confidence >= 0 ? confidence.ToString("0.00") : "n/a";
            AppendLog($"Voice heard: \"{phrase}\" (p={confidenceText})");

            if (ContainsCommandToken(normalized, "start") || normalized.Contains("begin animation training"))
            {
                BeginTraining();
                return;
            }

            if (ContainsCommandToken(normalized, "stop") || normalized.Contains("stop animation training"))
            {
                StopAndSave();
                return;
            }

            string replayPhrase = (_replayPhraseEntry.Text ?? string.Empty).Trim().ToLowerInvariant();
            if (replayPhrase.Length > 0 && normalized.Contains(replayPhrase))
            {
                ReplayPhrase();
            }
        });
    }

    void OnListenWindowStarted()
    {
        Application.Invoke(delegate
        {
            _voiceStatusLabel.Text = "Voice: recording (5s)";
            PlayListenWindowTone();
        });
    }

    void OnListenWindowEnded()
    {
        Application.Invoke(delegate
        {
            _voiceStatusLabel.Text = _voice.IsRunning ? "Voice: listening" : "Voice: off";
            PlayListenWindowTone();
        });
    }

    static bool ContainsCommandToken(string phrase, string token)
    {
        string[] parts = phrase.Split(new[] { ' ', '\t', '\r', '\n', '.', ',', ';', '!', '?', ':' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(p => string.Equals(p, token, StringComparison.OrdinalIgnoreCase));
    }

    bool TryGetCaptureInterval(out int intervalMs)
    {
        if (int.TryParse(_stepIntervalEntry.Text, out intervalMs) && intervalMs >= 200)
            return true;

        intervalMs = 0;
        _statusLabel.Text = "Validation: fail";
        AppendLog("Invalid capture interval. Use >= 200 ms.");
        return false;
    }

    void StopCaptureTimer()
    {
        if (_captureTimerId == 0)
            return;

        GLib.Source.Remove(_captureTimerId);
        _captureTimerId = 0;
    }

    void StartCaptureTimer(int intervalMs)
    {
        StopCaptureTimer();
        _captureTimerId = GLib.Timeout.Add((uint)intervalMs, () =>
        {
            try
            {
                Dictionary<string, int> frame = _training.CaptureStep();
                _statusLabel.Text = $"Training: step {_training.CapturedFrameCount}";
                BeepSignal();
                LogFrame("Captured", frame);
                return true;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Training: failed";
                AppendLog($"Capture ERROR: {ex.Message}");
                StopCaptureTimer();
                return false;
            }
        });
    }

    static void BeepSignal()
    {
        try
        {
            Gdk.Display.Default?.Beep();
        }
        catch
        {
            try { Console.Beep(); } catch { }
        }
    }

    static void PlayListenWindowTone()
    {
        AudioCueService.PlayTone(1120, 130, repeats: 1);
    }

    static void PlayTrainingCompletedTone()
    {
        AudioCueService.PlayTone(1480, 140, repeats: 1);
        AudioCueService.PlayTone(1860, 200, repeats: 1);
    }

    void LogFrame(string prefix, Dictionary<string, int> frame)
    {
        string body = string.Join(", ", frame.Select(kv => $"{kv.Key}={kv.Value}"));
        AppendLog($"{prefix} [{_training.CapturedFrameCount}] {body}");
    }

    void AppendLog(string line)
    {
        _log.AppendLine(line);
        _log.AppendLine();
        if (_logView.Buffer == null)
            _logView.Buffer = new TextBuffer(new TextTagTable());
        _logView.Buffer.Text = _log.ToString();
    }
}
