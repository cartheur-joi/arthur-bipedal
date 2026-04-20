using Gtk;
using joi_gtk.Services;
using Cartheur.Animals.Robot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace joi_gtk;

public sealed class MainWindow : Window
{
    const string InitialLogMessage = "GTK# robot panel initialized.";
    readonly RobotControlService _robot = new();
    readonly IRobotNarrationService _narration = new RobotNarrationService();
    readonly IPersonalityRuntime _personality = PersonalityRuntimeFactory.Create();
    readonly StringBuilder _log = new(InitialLogMessage);
    bool _clearedInitialLog;

    readonly Label _statusLabel = new("Ready");
    readonly TextView _logView = new() { Editable = false, CursorVisible = false, Monospace = true, WrapMode = WrapMode.WordChar };
    readonly Entry _overloadThresholdEntry = new() { Text = "900", WidthChars = 6 };
    readonly Label _monitorSummaryLabel = new("Monitoring stopped");
    readonly CheckButton _showMotorLabelsToggle = new("Show Labels") { Active = true };
    readonly EventBox _safetyAlertBox = new();
    readonly Label _safetyAlertLabel = new("Safety normal");
    readonly ComboBoxText _sweepMotorSelection = new();
    readonly Entry _sweepLowEntry = new() { Text = "420", WidthChars = 6 };
    readonly Entry _sweepHighEntry = new() { Text = "620", WidthChars = 6 };
    readonly Entry _sweepDurationEntry = new() { Text = "1200", WidthChars = 6 };
    readonly Label _sweepStatusLabel = new("Sweep idle");
    readonly ListStore _monitorStore = new(
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string));
    readonly Dictionary<string, EventBox> _motorIndicators = new();
    readonly List<Widget> _motorLabelWidgets = new();
    readonly Dictionary<EventBox, CssProvider> _eventBoxCssProviders = new();
    readonly Frame _monitoringFrame = new("Robot Monitoring");
    Window _robotMonitoringWindow;
    AnimationTrainingWindow _animationTrainingWindow;
    CameraWindow _cameraWindow;
    uint _monitorTimerId;
    string _lastAlertFingerprint = string.Empty;
    bool _flashPhase;
    bool _monitorPollInProgress;
    bool _safetyAlertActive;
    DateTime _lastSafetyAlarmUtc = DateTime.MinValue;
    bool _safetyToneInFlight;
    CancellationTokenSource _sweepCancellation;
    bool _sweepInProgress;
    int _eventBoxStyleSeed;

    public MainWindow() : base("Arthur Bipedal Robot Control Panel")
    {
        SetWmclass("arthur-bipedal", "ArthurBipedal");
        GtkWindowIconService.Apply(this);
        SetDefaultSize(1200, 700);
        Resizable = false;
        BorderWidth = 12;
        _robot.SafetyGateTripped += OnSafetyGateTripped;
        DeleteEvent += (_, _) =>
        {
            StopSweep();
            StopMonitoring();
            _robotMonitoringWindow?.Destroy();
            _robot.SafetyGateTripped -= OnSafetyGateTripped;
            if (_narration is IDisposable disposableNarration)
                disposableNarration.Dispose();
            Application.Quit();
        };

        Box root = new(Orientation.Vertical, 8);
        Add(root);

        MenuBar menuBar = BuildMenuBar();
        root.PackStart(menuBar, false, false, 0);

        Label title = new("Arthur Bipedal Robot Control Panel");
        title.Xalign = 0;
        root.PackStart(title, false, false, 0);

        root.PackStart(BuildControlPanel(), false, false, 0);

        Label policyLabel = new("Policy: Program poses and gait routines through Animation Training only.")
        {
            Xalign = 0
        };
        root.PackStart(policyLabel, false, false, 0);

        Box statusRow = new(Orientation.Horizontal, 8);
        statusRow.PackStart(new Label("Status:") { Xalign = 0 }, false, false, 0);
        statusRow.PackStart(_statusLabel, false, false, 0);
        root.PackStart(statusRow, false, false, 0);

        Frame consoleFrame = new("Console");
        ScrolledWindow scroll = new()
        {
            HscrollbarPolicy = PolicyType.Automatic,
            VscrollbarPolicy = PolicyType.Automatic
        };
        scroll.Add(_logView);
        consoleFrame.Add(scroll);
        root.PackStart(consoleFrame, true, true, 0);

        SetLog(_log.ToString());
        AppendLog($"[Voice] {_narration.Status}");
        AppendLog($"[Personality] {_personality.Status}");
    }

    static Button CreateButton(string text, EventHandler onClick)
    {
        Button button = new(text);
        button.Clicked += onClick;
        return button;
    }

    Widget BuildControlPanel()
    {
        Box panel = new(Orientation.Vertical, 6);

        Frame trainingFrame = new("Training & Views");
        Box trainingRow = new(Orientation.Horizontal, 8);
        trainingRow.BorderWidth = 6;
        trainingRow.PackStart(CreateButton("Initialize", (_, _) => RunAction("Initialize", () => _robot.Initialize())), false, false, 0);
        trainingRow.PackStart(CreateButton("Animation Training", (_, _) => OpenAnimationTrainingWindow()), false, false, 0);
        trainingRow.PackStart(CreateButton("View Robot Monitor", (_, _) => ShowRobotMonitor()), false, false, 0);
        trainingRow.PackStart(CreateButton("Camera Feed", (_, _) => OpenCameraWindow()), false, false, 0);
        trainingFrame.Add(trainingRow);
        panel.PackStart(trainingFrame, false, false, 0);

        Frame diagnosticFrame = new("Diagnostics");
        Box diagnosticRow = new(Orientation.Horizontal, 8);
        diagnosticRow.BorderWidth = 6;
        diagnosticRow.PackStart(CreateButton("Body Calibrate", (_, _) => RunAction("BodyCalibrate", () => _robot.RunStartupBodyAwarenessCalibration(strict: true))), false, false, 0);
        diagnosticRow.PackStart(CreateButton("Read IMU", (_, _) => RunAction("ReadIMU", () => _robot.ReadImuTelemetry())), false, false, 0);
        diagnosticRow.PackStart(CreateButton("Balance Step", (_, _) => RunAction("BalanceStep", () => _robot.ApplyStandingBalanceCompensationStep())), false, false, 0);
        diagnosticRow.PackStart(CreateButton("Read Lower Telemetry", (_, _) => RunAction("ReadLowerTelemetry", () => _robot.ReadLowerTelemetry())), false, false, 0);
        diagnosticFrame.Add(diagnosticRow);
        panel.PackStart(diagnosticFrame, false, false, 0);

        Frame runtimeFrame = new("Runtime");
        Box runtimeRow = new(Orientation.Horizontal, 8);
        runtimeRow.BorderWidth = 6;
        runtimeRow.PackStart(CreateButton("Full Self Check", (_, _) => RunAction("FullSelfCheck", RunFullSelfCheck)), false, false, 0);
        runtimeRow.PackStart(CreateButton("Interactive Toys (Text)", (_, _) => RunAction("InteractiveToysOnce", () =>
        {
            using InteractiveToysRuntime runtime = new();
            return runtime.RunSingleTurn("hello robot");
        })), false, false, 0);
        runtimeFrame.Add(runtimeRow);
        panel.PackStart(runtimeFrame, false, false, 0);

        Frame safetyFrame = new("Safety");
        Box safetyRow = new(Orientation.Horizontal, 8);
        safetyRow.BorderWidth = 6;
        safetyRow.PackStart(CreateButton("Torque ON (Lower)", (_, _) => RunAction("TorqueOnLower", () => _robot.TorqueOnLower())), false, false, 0);
        safetyRow.PackStart(CreateButton("Torque OFF (Lower)", (_, _) => RunAction("TorqueOffLower", () => _robot.TorqueOffLower())), false, false, 0);
        safetyRow.PackStart(CreateButton("Emergency Stop", (_, _) => RunAction("EmergencyStop", () => _robot.EmergencyStopLower())), false, false, 0);
        safetyRow.PackStart(CreateButton("Clear Log", (_, _) => ClearLogs()), false, false, 0);
        safetyFrame.Add(safetyRow);
        panel.PackStart(safetyFrame, false, false, 0);

        return panel;
    }

    MenuBar BuildMenuBar()
    {
        MenuBar menuBar = new();

        MenuItem fileMenuItem = new("_File");
        Menu fileMenu = new();
        MenuItem clearItem = new("Clear");
        clearItem.Activated += (_, _) => ClearLogs();
        MenuItem quitItem = new("Quit");
        quitItem.Activated += (_, _) => Application.Quit();
        fileMenu.Append(clearItem);
        fileMenu.Append(new SeparatorMenuItem());
        fileMenu.Append(quitItem);
        fileMenuItem.Submenu = fileMenu;

        MenuItem editMenuItem = new("_Edit");
        Menu editMenu = new();
        MenuItem copyLogItem = new("Copy Log");
        copyLogItem.Activated += (_, _) =>
        {
            Clipboard clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            clipboard.Text = _log.ToString();
        };
        editMenu.Append(copyLogItem);
        editMenuItem.Submenu = editMenu;

        MenuItem helpMenuItem = new("_Help");
        Menu helpMenu = new();
        MenuItem aboutItem = new("About");
        aboutItem.Activated += (_, _) =>
        {
            Dialog dialog = new("About", this, DialogFlags.Modal);
            dialog.AddButton("OK", ResponseType.Ok);
            dialog.SetDefaultSize(520, 180);
            dialog.Resizable = false;

            Label label = new(
                "Arthur Bipedal Controller.\nCopyright 2021 - 2026 Cartheur Research, B.V.\nAll rights reserved.")
            {
                Justify = Justification.Center,
                Xalign = 0.5f
            };

            dialog.ContentArea.BorderWidth = 16;
            dialog.ContentArea.PackStart(label, true, true, 0);
            dialog.ShowAll();
            dialog.Run();
            dialog.Destroy();
        };
        helpMenu.Append(aboutItem);
        helpMenuItem.Submenu = helpMenu;

        menuBar.Append(fileMenuItem);
        menuBar.Append(editMenuItem);
        menuBar.Append(helpMenuItem);
        return menuBar;
    }

    void ValidationFail(string message)
    {
        _statusLabel.Text = "Validation: FAIL";
        string line = $"[Validation] {message}";
        AppendLog(line);
        WriteConsoleEntry(line);
    }

    void RunAction(string actionName, Func<string> action)
    {
        AnnounceIntent(actionName);
        try
        {
            string result = action();
            _statusLabel.Text = $"{actionName}: OK";
            string line = $"[{actionName}] {result}";
            AppendLog(line);
            WriteConsoleEntry(line);
            AnnounceCompletion(actionName);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"{actionName}: FAIL";
            string line = $"[{actionName}] ERROR: {ex.Message}";
            AppendLog(line);
            WriteConsoleEntry(line);
            AnnounceFailure(actionName);
        }
    }

    void AnnounceIntent(string actionName)
    {
        if (!_narration.IsAvailable)
            return;

        string phrase = actionName switch
        {
            "BodyCalibrate" => "I will calibrate body awareness now.",
            "Initialize" => "I will initialize motor systems now.",
            "EmergencyStop" => "I will apply emergency stop torque off.",
            _ => $"I will execute {actionName}."
        };

        string spoken = _personality.AdaptSpeech(phrase, actionName);
        _ = Task.Run(() => _narration.Announce(spoken));
    }

    void AnnounceCompletion(string actionName)
    {
        if (!_narration.IsAvailable)
            return;

        string spoken = _personality.AdaptSpeech($"{actionName} completed.", actionName);
        _ = Task.Run(() => _narration.Announce(spoken));
    }

    void AnnounceFailure(string actionName)
    {
        if (!_narration.IsAvailable)
            return;

        string spoken = _personality.AdaptSpeech($"{actionName} failed. Please check safety status.", actionName);
        _ = Task.Run(() => _narration.Announce(spoken));
    }

    void AppendLog(string line)
    {
        if (!_clearedInitialLog)
        {
            _log.Clear();
            _clearedInitialLog = true;
        }

        _log.AppendLine(line);
        _log.AppendLine();
        SetLog(_log.ToString());
    }

    void SetLog(string text)
    {
        if (_logView.Buffer == null)
            _logView.Buffer = new TextBuffer(new TextTagTable());

        _logView.Buffer.Text = text;
    }

    void ClearLogs()
    {
        _log.Clear();
        _clearedInitialLog = true;
        SetLog(string.Empty);
        Console.Write("\u001b[2J\u001b[H");
    }

    static void WriteConsoleEntry(string line)
    {
        Console.WriteLine();
        Console.WriteLine(line);
    }

    static string RunFullSelfCheck()
    {
        FullSelfCheckService service = new();
        return service.Run();
    }

    void ShowRobotMonitor()
    {
        if (_monitoringFrame.Child == null)
        {
            _monitoringFrame.Add(BuildMonitoringPanelContent());
        }

        if (_robotMonitoringWindow == null)
        {
            _robotMonitoringWindow = new Window("Robot Monitoring")
            {
                TransientFor = this
            };
            _robotMonitoringWindow.SetWmclass("arthur-bipedal", "ArthurBipedal");
            GtkWindowIconService.Apply(_robotMonitoringWindow);
            _robotMonitoringWindow.SetDefaultSize(1180, 760);
            _robotMonitoringWindow.Resizable = false;
            _robotMonitoringWindow.Add(_monitoringFrame);
            _robotMonitoringWindow.DeleteEvent += (_, e) =>
            {
                _robotMonitoringWindow.Hide();
                e.RetVal = true;
            };
        }

        _robotMonitoringWindow.ShowAll();
        _robotMonitoringWindow.Present();

        RunMonitoringSnapshot("ViewRobotMonitor", true);
    }

    void OpenAnimationTrainingWindow()
    {
        try
        {
            if (_animationTrainingWindow == null || !_animationTrainingWindow.Visible)
            {
                _animationTrainingWindow = new AnimationTrainingWindow(_robot)
                {
                    TransientFor = this
                };
                _animationTrainingWindow.ShowAll();
                return;
            }

            _animationTrainingWindow.Present();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "AnimationTraining: FAIL";
            string line = $"[AnimationTraining] ERROR: {ex.Message}";
            AppendLog(line);
            WriteConsoleEntry(line);
        }
    }

    void OpenCameraWindow()
    {
        try
        {
            if (_cameraWindow == null || !_cameraWindow.Visible)
            {
                _cameraWindow = new CameraWindow
                {
                    TransientFor = this
                };
                _cameraWindow.ShowAll();
                return;
            }

            _cameraWindow.Present();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Camera: FAIL";
            string line = $"[Camera] ERROR: {ex.Message}";
            AppendLog(line);
            WriteConsoleEntry(line);
        }
    }

    Box BuildMonitoringPanelContent()
    {
        Box container = new(Orientation.Vertical, 8);
        container.BorderWidth = 8;

        InitializeSafetyAlertBox();
        _safetyAlertBox.Hide();
        container.PackStart(_safetyAlertBox, false, false, 0);

        Box controls = new(Orientation.Horizontal, 8);
        controls.PackStart(new Label("Overload threshold") { Xalign = 0 }, false, false, 0);
        controls.PackStart(_overloadThresholdEntry, false, false, 0);
        controls.PackStart(_showMotorLabelsToggle, false, false, 0);
        controls.PackStart(CreateButton("Refresh Snapshot", (_, _) => RunMonitoringSnapshot("ManualRefresh", false)), false, false, 0);
        controls.PackStart(CreateButton("Start Monitoring", (_, _) => StartMonitoring()), false, false, 0);
        controls.PackStart(CreateButton("Stop Monitoring", (_, _) => StopMonitoring()), false, false, 0);
        controls.PackStart(CreateButton("Acknowledge Alert", (_, _) => AcknowledgeSafetyAlert()), false, false, 0);
        controls.PackStart(_monitorSummaryLabel, false, false, 12);
        container.PackStart(controls, false, false, 0);

        Frame sweepFrame = BuildSafeSweepPanel();
        container.PackStart(sweepFrame, false, false, 0);

        Paned body = new(Orientation.Horizontal);
        body.WideHandle = true;
        body.Position = 520;

        Frame mapFrame = BuildMotorMapPanel();
        body.Pack1(mapFrame, true, false);

        TreeView view = new(_monitorStore) { HeadersVisible = true };
        view.AppendColumn("Motor", new CellRendererText(), "text", 0);
        view.AppendColumn("ID", new CellRendererText(), "text", 1);
        view.AppendColumn("Zone", new CellRendererText(), "text", 2);
        view.AppendColumn("Torque", new CellRendererText(), "text", 3);
        view.AppendColumn("Load", new CellRendererText(), "text", 4);
        view.AppendColumn("Status", new CellRendererText(), "text", 5);

        ScrolledWindow tableScroll = new()
        {
            HscrollbarPolicy = PolicyType.Automatic,
            VscrollbarPolicy = PolicyType.Automatic,
            HeightRequest = 260
        };
        tableScroll.Add(view);
        body.Pack2(tableScroll, true, false);
        container.PackStart(body, true, true, 0);

        return container;
    }

    Frame BuildSafeSweepPanel()
    {
        Frame frame = new("Safe Sweep");
        Box row = new(Orientation.Horizontal, 8);
        row.BorderWidth = 6;

        PopulateSweepMotorSelection();
        row.PackStart(new Label("Motor") { Xalign = 0 }, false, false, 0);
        row.PackStart(_sweepMotorSelection, false, false, 0);
        row.PackStart(new Label("Low") { Xalign = 0 }, false, false, 0);
        row.PackStart(_sweepLowEntry, false, false, 0);
        row.PackStart(new Label("High") { Xalign = 0 }, false, false, 0);
        row.PackStart(_sweepHighEntry, false, false, 0);
        row.PackStart(new Label("Step ms") { Xalign = 0 }, false, false, 0);
        row.PackStart(_sweepDurationEntry, false, false, 0);
        row.PackStart(CreateButton("Start Sweep", (_, _) => StartSweep()), false, false, 0);
        row.PackStart(CreateButton("Stop Sweep", (_, _) => StopSweep()), false, false, 0);
        row.PackStart(_sweepStatusLabel, false, false, 8);
        frame.Add(row);
        return frame;
    }

    void PopulateSweepMotorSelection()
    {
        if (_sweepMotorSelection.Model != null)
            return;

        if (Motor.MotorContext == null || Motor.MotorContext.Count == 0)
            MotorFunctions.CollateMotorArray();

        foreach (string motor in Motor.MotorContext
                     .OrderBy(kv => kv.Value)
                     .Select(kv => kv.Key))
        {
            _sweepMotorSelection.AppendText(motor);
        }

        if (_sweepMotorSelection.Active < 0)
            _sweepMotorSelection.Active = 0;
    }

    void StartSweep()
    {
        if (_sweepInProgress)
            return;

        if (_monitorTimerId == 0)
        {
            ValidationFail("Start Monitoring before running Safe Sweep.");
            return;
        }

        string motor = _sweepMotorSelection.ActiveText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(motor))
        {
            ValidationFail("Select a motor for Safe Sweep.");
            return;
        }

        if (!int.TryParse(_sweepLowEntry.Text, out int lowTarget) ||
            !int.TryParse(_sweepHighEntry.Text, out int highTarget) ||
            !int.TryParse(_sweepDurationEntry.Text, out int stepMs))
        {
            ValidationFail("Invalid Safe Sweep values.");
            return;
        }
        if (lowTarget < 0 || highTarget > 4095 || highTarget <= lowTarget)
        {
            ValidationFail("Safe Sweep range must be 0..4095 and low < high.");
            return;
        }
        if (stepMs < 200)
        {
            ValidationFail("Safe Sweep step duration must be >= 200 ms.");
            return;
        }

        _sweepCancellation = new CancellationTokenSource();
        _sweepInProgress = true;
        _sweepStatusLabel.Text = $"Sweeping {motor}";
        AppendLog($"[Sweep] Started motor={motor} low={lowTarget} high={highTarget} stepMs={stepMs}");
        _ = Task.Run(() => RunSweepLoop(motor, lowTarget, highTarget, stepMs, _sweepCancellation.Token));
    }

    void StopSweep()
    {
        if (!_sweepInProgress)
            return;

        _sweepCancellation?.Cancel();
        _sweepStatusLabel.Text = "Sweep stopping...";
    }

    void RunSweepLoop(string motor, int lowTarget, int highTarget, int stepMs, CancellationToken token)
    {
        int originalPosition = 0;
        bool hasOriginal = false;
        Exception failure = null;
        try
        {
            originalPosition = _robot.ReadPositions(new[] { motor })[motor];
            hasOriginal = true;
            _robot.SetTorqueOn(new[] { motor });

            int[] targets = { lowTarget, highTarget };
            int index = 0;
            while (!token.IsCancellationRequested)
            {
                if (_monitorTimerId == 0)
                    throw new InvalidOperationException("Safe Sweep stopped because monitoring is no longer active.");

                int target = targets[index];
                _robot.MoveToPositions(new Dictionary<string, int> { [motor] = target }, stepMs, 10);
                int measured = _robot.ReadPositions(new[] { motor })[motor];
                Application.Invoke(delegate
                {
                    AppendLog($"[Sweep] motor={motor} target={target} measured={measured}");
                });

                index = (index + 1) % targets.Length;
                Thread.Sleep(100);
            }
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            if (hasOriginal)
            {
                try
                {
                    _robot.MoveToPositions(new Dictionary<string, int> { [motor] = originalPosition }, stepMs, 10);
                    Application.Invoke(delegate
                    {
                        AppendLog($"[Sweep] Returned motor={motor} to origin={originalPosition}");
                    });
                }
                catch (Exception ex)
                {
                    if (failure == null)
                        failure = ex;
                }
            }

            Application.Invoke(delegate
            {
                _sweepInProgress = false;
                _sweepCancellation?.Dispose();
                _sweepCancellation = null;

                if (failure == null || token.IsCancellationRequested)
                {
                    _sweepStatusLabel.Text = "Sweep idle";
                    if (failure == null)
                        AppendLog($"[Sweep] Stopped motor={motor}");
                }
                else
                {
                    _sweepStatusLabel.Text = "Sweep failed";
                    string line = $"[Sweep] ERROR motor={motor}: {failure.Message}";
                    AppendLog(line);
                    WriteConsoleEntry(line);
                }
            });
        }
    }

    Frame BuildMotorMapPanel()
    {
        if (Motor.MotorContext == null || Motor.MotorContext.Count == 0)
            MotorFunctions.CollateMotorArray();

        Frame frame = new("Motor Map");
        ScrolledWindow scroll = new()
        {
            HscrollbarPolicy = PolicyType.Automatic,
            VscrollbarPolicy = PolicyType.Automatic
        };

        Fixed canvas = new();
        Image mapImage = new(ResolveMotorMapImagePath());
        canvas.Put(mapImage, 0, 0);

        foreach ((string motor, int x, int y) in GetMotorIndicatorCoordinates())
        {
            EventBox indicator = new();
            indicator.SetSizeRequest(14, 14);
            indicator.TooltipText = $"{motor} ({Motor.ReturnID(motor)})";
            SetIndicatorColor(indicator, IndicatorState.Normal);
            canvas.Put(indicator, x, y);
            _motorIndicators[motor] = indicator;

            Widget labelWidget = BuildMotorLabelWidget(motor, x);
            canvas.Put(labelWidget, ResolveLabelX(x), y - 2);
            _motorLabelWidgets.Add(labelWidget);
        }

        _showMotorLabelsToggle.Toggled += (_, _) => UpdateMotorLabelVisibility();
        UpdateMotorLabelVisibility();

        scroll.Add(canvas);
        frame.Add(scroll);
        return frame;
    }

    Widget BuildMotorLabelWidget(string motor, int indicatorX)
    {
        EventBox tag = new();
        SetEventBoxColor(tag, 0.10, 0.10, 0.10);

        Label label = new()
        {
            Xalign = indicatorX >= 300 ? 0f : 1f,
            UseMarkup = true,
            Markup = $"<span foreground=\"white\" size=\"small\">{GLib.Markup.EscapeText(motor)}</span>"
        };
        tag.Add(label);
        return tag;
    }

    void UpdateMotorLabelVisibility()
    {
        bool visible = _showMotorLabelsToggle.Active;
        foreach (Widget widget in _motorLabelWidgets)
        {
            if (visible)
                widget.ShowAll();
            else
                widget.Hide();
        }
    }

    static int ResolveLabelX(int indicatorX)
    {
        if (indicatorX >= 300)
            return indicatorX + 16;
        return Math.Max(4, indicatorX - 100);
    }

    static string ResolveMotorMapImagePath()
    {
        string runtimeCopy = System.IO.Path.Combine(AppContext.BaseDirectory, "images", "motors_on_robot.png");
        if (System.IO.File.Exists(runtimeCopy))
            return runtimeCopy;

        string workspaceImage = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "images", "motors_on_robot.png"));
        if (System.IO.File.Exists(workspaceImage))
            return workspaceImage;

        throw new FileNotFoundException("Missing robot map image (motors_on_robot.png).", runtimeCopy);
    }

    static IReadOnlyList<(string motor, int x, int y)> GetMotorIndicatorCoordinates()
    {
        return new List<(string motor, int x, int y)>
        {
            ("head_y", 394, 48), ("head_z", 262, 206),
            ("l_shoulder_y", 452, 88), ("l_shoulder_x", 474, 128), ("l_arm_z", 496, 168), ("l_elbow_y", 536, 220),
            ("r_shoulder_y", 154, 94), ("r_shoulder_x", 126, 128), ("r_arm_z", 98, 168), ("r_elbow_y", 62, 216),
            ("bust_x", 418, 242), ("bust_y", 256, 244), ("abs_y", 228, 286), ("abs_z", 392, 286), ("abs_x", 380, 326),
            ("l_hip_z", 436, 352), ("l_hip_y", 426, 392), ("l_hip_x", 418, 432), ("l_knee_y", 496, 558), ("l_ankle_y", 524, 650),
            ("r_hip_z", 234, 352), ("r_hip_y", 224, 392), ("r_hip_x", 214, 432), ("r_knee_y", 144, 558), ("r_ankle_y", 120, 650)
        };
    }

    enum IndicatorState
    {
        Normal,
        OverloadOn,
        OverloadOff,
        CommError
    }

    void SetIndicatorColor(EventBox indicator, IndicatorState state)
    {
        (double red, double green, double blue) = state switch
        {
            IndicatorState.OverloadOn => (0.95, 0.10, 0.10),
            IndicatorState.OverloadOff => (0.45, 0.08, 0.08),
            IndicatorState.CommError => (0.95, 0.55, 0.00),
            _ => (0.72, 0.72, 0.72)
        };

        SetEventBoxColor(indicator, red, green, blue);
    }

    void SetEventBoxColor(EventBox target, double red, double green, double blue)
    {
        if (!_eventBoxCssProviders.TryGetValue(target, out CssProvider provider))
        {
            provider = new CssProvider();
            _eventBoxCssProviders[target] = provider;
            target.StyleContext.AddProvider(provider, (uint)StyleProviderPriority.Application);
        }

        if (string.IsNullOrWhiteSpace(target.Name))
            target.Name = $"eventbox-{Interlocked.Increment(ref _eventBoxStyleSeed)}";

        int r = ClampByte(red);
        int g = ClampByte(green);
        int b = ClampByte(blue);
        string css = $"#{target.Name} {{ background-color: rgba({r}, {g}, {b}, 1.0); }}";
        provider.LoadFromData(css);
    }

    static int ClampByte(double value)
    {
        int scaled = (int)Math.Round(value * 255.0, MidpointRounding.AwayFromZero);
        if (scaled < 0) return 0;
        if (scaled > 255) return 255;
        return scaled;
    }

    void StartMonitoring()
    {
        if (_monitorTimerId != 0)
            return;

        RunMonitoringSnapshot("MonitoringStart", true);
        _monitorTimerId = GLib.Timeout.Add(750, () =>
        {
            RunMonitoringSnapshot("MonitoringTick", false);
            return true;
        });
    }

    void StopMonitoring()
    {
        if (_monitorTimerId == 0)
            return;

        GLib.Source.Remove(_monitorTimerId);
        _monitorTimerId = 0;
        _monitorSummaryLabel.Text = "Monitoring stopped";
        _flashPhase = false;
        ResetIndicators();
    }

    void RunMonitoringSnapshot(string actionName, bool logWhenNoAlert)
    {
        if (!TryGetOverloadThreshold(out int threshold))
            return;

        if (_monitorPollInProgress)
            return;

        _monitorPollInProgress = true;
        _ = Task.Run(() =>
        {
            try
            {
                IReadOnlyList<MotorMonitorReading> snapshot = _robot.ReadMotorMonitoringSnapshot(threshold);
                Application.Invoke(delegate
                {
                    RenderSnapshot(snapshot);

                    var overloads = snapshot
                        .Where(r => r.Overload)
                        .Select(r => $"{r.MotorName}({r.Load})")
                        .ToArray();
                    var thermalViolations = snapshot
                        .Where(r => r.ThermalViolation)
                        .Select(r => $"{r.MotorName}({r.Temperature}>={r.MaxTemperature})")
                        .ToArray();
                    var voltageViolations = snapshot
                        .Where(r => r.VoltageViolation)
                        .Select(r => $"{r.MotorName}({r.Voltage}<={r.MinVoltage})")
                        .ToArray();

                    string alertFingerprint = string.Join(
                        "|",
                        overloads
                            .Select(value => "O:" + value)
                            .Concat(thermalViolations.Select(value => "T:" + value))
                            .Concat(voltageViolations.Select(value => "V:" + value)));
                    if (overloads.Length > 0 || thermalViolations.Length > 0 || voltageViolations.Length > 0)
                    {
                        _statusLabel.Text = "Monitoring: ALERT";
                        _monitorSummaryLabel.Text =
                            $"Alerts: overload={overloads.Length}, thermal={thermalViolations.Length}, voltage={voltageViolations.Length}";
                        if (_lastAlertFingerprint != alertFingerprint)
                        {
                            string line =
                                $"[{actionName}] ALERT overload=[{string.Join(", ", overloads.DefaultIfEmpty("none"))}] " +
                                $"thermal=[{string.Join(", ", thermalViolations.DefaultIfEmpty("none"))}] " +
                                $"voltage=[{string.Join(", ", voltageViolations.DefaultIfEmpty("none"))}]";
                            AppendLog(line);
                            WriteConsoleEntry(line);
                        }
                    }
                    else
                    {
                        _statusLabel.Text = "Monitoring: OK";
                        _monitorSummaryLabel.Text = "No overloads";
                        if (logWhenNoAlert)
                        {
                            string line = $"[{actionName}] Snapshot OK ({snapshot.Count} motors).";
                            AppendLog(line);
                            WriteConsoleEntry(line);
                        }
                    }

                    _lastAlertFingerprint = alertFingerprint;
                });
            }
            catch (Exception ex)
            {
                Application.Invoke(delegate
                {
                    _statusLabel.Text = "Monitoring: FAIL";
                    _monitorSummaryLabel.Text = "Monitoring error";
                    string line = $"[{actionName}] ERROR: {ex.Message}";
                    AppendLog(line);
                    WriteConsoleEntry(line);
                    StopMonitoring();
                });
            }
            finally
            {
                _monitorPollInProgress = false;
            }
        });
    }

    bool TryGetOverloadThreshold(out int threshold)
    {
        if (int.TryParse(_overloadThresholdEntry.Text, out threshold) && threshold > 0)
            return true;

        ValidationFail("Invalid overload threshold.");
        return false;
    }

    void RenderSnapshot(IReadOnlyList<MotorMonitorReading> snapshot)
    {
        _flashPhase = !_flashPhase;
        _monitorStore.Clear();
        HashSet<string> seen = new();
        foreach (MotorMonitorReading motor in snapshot)
        {
            string status = motor.CommunicationOk
                ? motor.Overload ? "OVERLOAD" : "OK"
                : $"COMM ERR: {motor.Error}";

            _monitorStore.AppendValues(
                motor.MotorName,
                motor.ID.ToString(),
                motor.Location,
                motor.TorqueOn ? "ON" : "OFF",
                motor.Load.ToString(),
                status);

            if (_motorIndicators.TryGetValue(motor.MotorName, out EventBox indicator))
            {
                IndicatorState state = !motor.CommunicationOk
                    ? IndicatorState.CommError
                    : motor.Overload
                    ? (_flashPhase ? IndicatorState.OverloadOn : IndicatorState.OverloadOff)
                    : IndicatorState.Normal;
                SetIndicatorColor(indicator, state);
                seen.Add(motor.MotorName);
            }
        }

        foreach ((string motor, EventBox indicator) in _motorIndicators)
        {
            if (!seen.Contains(motor))
                SetIndicatorColor(indicator, IndicatorState.Normal);
        }
    }

    void ResetIndicators()
    {
        foreach (EventBox indicator in _motorIndicators.Values)
            SetIndicatorColor(indicator, IndicatorState.Normal);
    }

    void InitializeSafetyAlertBox()
    {
        _safetyAlertLabel.Xalign = 0;
        _safetyAlertLabel.UseMarkup = true;
        _safetyAlertLabel.Markup = "<b>Safety normal</b>";
        _safetyAlertBox.BorderWidth = 6;
        _safetyAlertBox.Add(_safetyAlertLabel);
        SetEventBoxColor(_safetyAlertBox, 0.75, 0.12, 0.12);
    }

    void OnSafetyGateTripped(SafetyGateTrip trip)
    {
        Application.Invoke(delegate
        {
            string scopeText = trip.Scope.Count == 0 ? "all" : string.Join(",", trip.Scope);
            string line = $"[SafetyGate] TRIPPED action={trip.ActionName} phase={trip.Phase} scope={scopeText} {trip.Detail}";
            ApplySafetyAlert(line);
            RunMonitoringSnapshot("SafetyGateTrip", false);
        });
    }

    void ApplySafetyAlert(string message)
    {
        if (!_monitoringFrame.Visible)
            _monitoringFrame.ShowAll();

        _safetyAlertActive = true;
        _statusLabel.Text = "SAFETY: TRIPPED";
        _monitorSummaryLabel.Text = "Safety trip active";
        _safetyAlertLabel.Markup = $"<b>{GLib.Markup.EscapeText(message)}</b>";
        _safetyAlertBox.ShowAll();
        AppendLog(message);
        WriteConsoleEntry(message);
        BeepSafetyAlarm();
    }

    void AcknowledgeSafetyAlert()
    {
        if (!_safetyAlertActive)
            return;

        _safetyAlertActive = false;
        _safetyAlertBox.Hide();
        _safetyAlertLabel.Markup = "<b>Safety normal</b>";
        _monitorSummaryLabel.Text = "Alert acknowledged";
        AppendLog("[SafetyGate] Alert acknowledged.");
    }

    void BeepSafetyAlarm()
    {
        DateTime now = DateTime.UtcNow;
        if ((now - _lastSafetyAlarmUtc).TotalMilliseconds < 900)
            return;

        _lastSafetyAlarmUtc = now;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Beep(1240, 180);
                Console.Beep(880, 220);
            }
            else
            {
                PlayLinuxTwoToneAlarm();
            }
        }
        catch
        {
            try { Gdk.Display.Default?.Beep(); } catch { }
        }
    }

    void PlayLinuxTwoToneAlarm()
    {
        if (_safetyToneInFlight)
            return;

        _safetyToneInFlight = true;
        _ = Task.Run(() =>
        {
            try
            {
                PlayTone(1240, 0.18);
                System.Threading.Thread.Sleep(100);
                PlayTone(880, 0.22);
            }
            catch
            {
                try { Gdk.Display.Default?.Beep(); } catch { }
            }
            finally
            {
                _safetyToneInFlight = false;
            }
        });
    }

    static void PlayTone(int frequency, double durationSeconds)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "ffplay",
            Arguments = $"-v error -nodisp -autoexit -f lavfi \"sine=frequency={frequency}:duration={durationSeconds:0.00}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (!process.Start())
            throw new InvalidOperationException("ffplay failed to start.");
        process.WaitForExit(1200);
    }
}
