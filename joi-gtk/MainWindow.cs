using Gtk;
using joi_gtk.Services;
using Cartheur.Animals.Robot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace joi_gtk;

public sealed class MainWindow : Window
{
    const string InitialLogMessage = "GTK# robot panel initialized.";
    readonly RobotControlService _robot = new();
    readonly StringBuilder _log = new(InitialLogMessage);
    bool _clearedInitialLog;

    readonly Label _statusLabel = new("Ready");
    readonly Entry _cyclesEntry = new() { Text = "1" };
    readonly Entry _stepDurationEntry = new() { Text = "800" };
    readonly Entry _interpolationEntry = new() { Text = "12" };
    readonly Entry _timeoutEntry = new() { Text = "8000" };
    readonly CheckButton _requireFootContact = new("Need foot-contact");
    readonly TextView _logView = new() { Editable = false, CursorVisible = false, Monospace = true, WrapMode = WrapMode.WordChar };
    readonly Entry _overloadThresholdEntry = new() { Text = "900", WidthChars = 6 };
    readonly Label _monitorSummaryLabel = new("Monitoring stopped");
    readonly ListStore _monitorStore = new(
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string));
    readonly Dictionary<string, EventBox> _motorIndicators = new();
    readonly Frame _monitoringFrame;
    AnimationTrainingWindow _animationTrainingWindow;
    uint _monitorTimerId;
    string _lastOverloadFingerprint = string.Empty;
    bool _flashPhase;

    public MainWindow() : base("Arthur Bipedal - Linux Control Panel (GTK#)")
    {
        SetDefaultSize(1200, 700);
        BorderWidth = 12;
        DeleteEvent += (_, _) =>
        {
            StopMonitoring();
            Application.Quit();
        };

        Box root = new(Orientation.Vertical, 8);
        Add(root);

        MenuBar menuBar = BuildMenuBar();
        root.PackStart(menuBar, false, false, 0);

        Label title = new("Arthur Bipedal - Linux Control Panel");
        title.Xalign = 0;
        root.PackStart(title, false, false, 0);

        Box actionRow = new(Orientation.Horizontal, 8);
        actionRow.PackStart(CreateButton("Initialize", (_, _) => RunAction("Initialize", () => _robot.Initialize())), false, false, 0);
        actionRow.PackStart(CreateButton("Animation Training", (_, _) => OpenAnimationTrainingWindow()), false, false, 0);
        actionRow.PackStart(CreateButton("View Robot Monitor", (_, _) => ShowRobotMonitor()), false, false, 0);
        actionRow.PackStart(CreateButton("Torque ON (Lower)", (_, _) => RunAction("TorqueOnLower", () => _robot.TorqueOnLower())), false, false, 0);
        actionRow.PackStart(CreateButton("Torque OFF (Lower)", (_, _) => RunAction("TorqueOffLower", () => _robot.TorqueOffLower())), false, false, 0);
        actionRow.PackStart(CreateButton("Read Lower Telemetry", (_, _) => RunAction("ReadLowerTelemetry", () => _robot.ReadLowerTelemetry())), false, false, 0);
        actionRow.PackStart(CreateButton("Clear", (_, _) => ClearLogs()), false, false, 0);
        actionRow.PackStart(new Label("Status:") { Xalign = 0 }, false, false, 10);
        actionRow.PackStart(_statusLabel, false, false, 0);
        root.PackStart(actionRow, false, false, 0);

        Grid walkRow = new()
        {
            ColumnSpacing = 8,
            RowSpacing = 8
        };
        walkRow.Attach(new Label("Cycles") { Xalign = 0 }, 0, 0, 1, 1);
        walkRow.Attach(_cyclesEntry, 1, 0, 1, 1);
        walkRow.Attach(new Label("Step ms") { Xalign = 0 }, 2, 0, 1, 1);
        walkRow.Attach(_stepDurationEntry, 3, 0, 1, 1);
        walkRow.Attach(new Label("Interp") { Xalign = 0 }, 4, 0, 1, 1);
        walkRow.Attach(_interpolationEntry, 5, 0, 1, 1);
        walkRow.Attach(new Label("Timeout ms") { Xalign = 0 }, 6, 0, 1, 1);
        walkRow.Attach(_timeoutEntry, 7, 0, 1, 1);
        walkRow.Attach(_requireFootContact, 8, 0, 1, 1);
        walkRow.Attach(CreateButton("Walk (Supervised)", (_, _) => ExecuteSupervisedWalk()), 9, 0, 1, 1);
        walkRow.Attach(CreateButton("Walk 3 Cycles", (_, _) => ExecuteThreeCycleWalk()), 10, 0, 1, 1);
        walkRow.Attach(CreateButton("Emergency Stop", (_, _) => RunAction("EmergencyStop", () => _robot.EmergencyStopLower())), 11, 0, 1, 1);
        root.PackStart(walkRow, false, false, 0);

        _monitoringFrame = BuildMonitoringPanel();
        _monitoringFrame.Hide();
        root.PackStart(_monitoringFrame, true, true, 0);

        ScrolledWindow scroll = new()
        {
            HscrollbarPolicy = PolicyType.Automatic,
            VscrollbarPolicy = PolicyType.Automatic,
            HeightRequest = 110
        };
        scroll.Add(_logView);
        root.PackStart(scroll, false, false, 0);

        SetLog(_log.ToString());
    }

    static Button CreateButton(string text, EventHandler onClick)
    {
        Button button = new(text);
        button.Clicked += onClick;
        return button;
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

    void ExecuteThreeCycleWalk()
    {
        _cyclesEntry.Text = "3";
        ExecuteSupervisedWalk();
    }

    void ExecuteSupervisedWalk()
    {
        if (!TryReadWalkInputs(out int cycles, out int stepDurationMs, out int interpolationSteps, out int timeoutMs))
            return;

        RunAction(
            "ExecuteSupervisedWalk",
            () => _robot.ExecuteWalkCycleSupervised(
                cycles,
                stepDurationMs,
                interpolationSteps,
                timeoutMs,
                _requireFootContact.Active));
    }

    bool TryReadWalkInputs(out int cycles, out int stepDurationMs, out int interpolationSteps, out int timeoutMs)
    {
        cycles = 0;
        stepDurationMs = 0;
        interpolationSteps = 0;
        timeoutMs = 0;

        if (!int.TryParse(_cyclesEntry.Text, out cycles) || cycles < 1)
        {
            ValidationFail("Invalid cycles value.");
            return false;
        }
        if (!int.TryParse(_stepDurationEntry.Text, out stepDurationMs) || stepDurationMs < 100)
        {
            ValidationFail("Invalid step duration (min 100 ms).");
            return false;
        }
        if (!int.TryParse(_interpolationEntry.Text, out interpolationSteps) || interpolationSteps < 1)
        {
            ValidationFail("Invalid interpolation step count.");
            return false;
        }
        if (!int.TryParse(_timeoutEntry.Text, out timeoutMs) || timeoutMs < 1000)
        {
            ValidationFail("Invalid timeout (min 1000 ms).");
            return false;
        }
        return true;
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
        try
        {
            string result = action();
            _statusLabel.Text = $"{actionName}: OK";
            string line = $"[{actionName}] {result}";
            AppendLog(line);
            WriteConsoleEntry(line);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"{actionName}: FAIL";
            string line = $"[{actionName}] ERROR: {ex.Message}";
            AppendLog(line);
            WriteConsoleEntry(line);
        }
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

    void ShowRobotMonitor()
    {
        if (!_monitoringFrame.Visible)
            _monitoringFrame.ShowAll();

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

    Frame BuildMonitoringPanel()
    {
        Frame frame = new("Robot Monitoring");

        Box container = new(Orientation.Vertical, 8);
        container.BorderWidth = 8;

        Box controls = new(Orientation.Horizontal, 8);
        controls.PackStart(new Label("Overload threshold") { Xalign = 0 }, false, false, 0);
        controls.PackStart(_overloadThresholdEntry, false, false, 0);
        controls.PackStart(CreateButton("Refresh Snapshot", (_, _) => RunMonitoringSnapshot("ManualRefresh", false)), false, false, 0);
        controls.PackStart(CreateButton("Start Monitoring", (_, _) => StartMonitoring()), false, false, 0);
        controls.PackStart(CreateButton("Stop Monitoring", (_, _) => StopMonitoring()), false, false, 0);
        controls.PackStart(_monitorSummaryLabel, false, false, 12);
        container.PackStart(controls, false, false, 0);

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

        frame.Add(container);
        return frame;
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
        }

        scroll.AddWithViewport(canvas);
        frame.Add(scroll);
        return frame;
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

    static void SetIndicatorColor(EventBox indicator, IndicatorState state)
    {
        (double red, double green, double blue) = state switch
        {
            IndicatorState.OverloadOn => (0.95, 0.10, 0.10),
            IndicatorState.OverloadOff => (0.45, 0.08, 0.08),
            IndicatorState.CommError => (0.95, 0.55, 0.00),
            _ => (0.72, 0.72, 0.72)
        };

        indicator.OverrideBackgroundColor(StateFlags.Normal, new Gdk.RGBA
        {
            Red = red,
            Green = green,
            Blue = blue,
            Alpha = 1.0
        });
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

        try
        {
            IReadOnlyList<MotorMonitorReading> snapshot = _robot.ReadMotorMonitoringSnapshot(threshold);
            RenderSnapshot(snapshot);

            var overloads = snapshot
                .Where(r => r.Overload)
                .Select(r => $"{r.MotorName}({r.Load})")
                .ToArray();

            string overloadFingerprint = string.Join("|", overloads);
            if (overloads.Length > 0)
            {
                _statusLabel.Text = "Monitoring: ALERT";
                _monitorSummaryLabel.Text = $"Overload(s): {overloads.Length}";
                if (_lastOverloadFingerprint != overloadFingerprint)
                {
                    string line = $"[{actionName}] OVERLOAD {string.Join(", ", overloads)}";
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

            _lastOverloadFingerprint = overloadFingerprint;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Monitoring: FAIL";
            _monitorSummaryLabel.Text = "Monitoring error";
            string line = $"[{actionName}] ERROR: {ex.Message}";
            AppendLog(line);
            WriteConsoleEntry(line);
            StopMonitoring();
        }
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
}
