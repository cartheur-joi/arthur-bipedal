using Gtk;
using joi_gtk.Services;
using System;
using System.Text;

namespace joi_gtk;

public sealed class MainWindow : Window
{
    readonly RobotControlService _robot = new();
    readonly StringBuilder _log = new("GTK# robot panel initialized.");

    readonly Label _statusLabel = new("Ready");
    readonly Entry _cyclesEntry = new() { Text = "1" };
    readonly Entry _stepDurationEntry = new() { Text = "800" };
    readonly Entry _interpolationEntry = new() { Text = "12" };
    readonly Entry _timeoutEntry = new() { Text = "8000" };
    readonly CheckButton _requireFootContact = new("Need foot-contact");
    readonly TextView _logView = new() { Editable = false, CursorVisible = false, Monospace = true };

    public MainWindow() : base("Arthur Bipedal - Linux Control Panel (GTK#)")
    {
        SetDefaultSize(1200, 700);
        BorderWidth = 12;
        DeleteEvent += (_, _) => Application.Quit();

        Box root = new(Orientation.Vertical, 8);
        Add(root);

        Label title = new("Arthur Bipedal - Linux Control Panel");
        title.Xalign = 0;
        root.PackStart(title, false, false, 0);

        Box actionRow = new(Orientation.Horizontal, 8);
        actionRow.PackStart(CreateButton("Initialize", (_, _) => RunAction("Initialize", () => _robot.Initialize())), false, false, 0);
        actionRow.PackStart(CreateButton("Torque ON (Lower)", (_, _) => RunAction("TorqueOnLower", () => _robot.TorqueOnLower())), false, false, 0);
        actionRow.PackStart(CreateButton("Torque OFF (Lower)", (_, _) => RunAction("TorqueOffLower", () => _robot.TorqueOffLower())), false, false, 0);
        actionRow.PackStart(CreateButton("Read Lower Telemetry", (_, _) => RunAction("ReadLowerTelemetry", () => _robot.ReadLowerTelemetry())), false, false, 0);
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

        ScrolledWindow scroll = new() { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
        scroll.Add(_logView);
        root.PackStart(scroll, true, true, 0);

        SetLog(_log.ToString());
    }

    static Button CreateButton(string text, EventHandler onClick)
    {
        Button button = new(text);
        button.Clicked += onClick;
        return button;
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
        AppendLog($"{DateTime.Now:HH:mm:ss} [Validation] {message}");
    }

    void RunAction(string actionName, Func<string> action)
    {
        try
        {
            string result = action();
            _statusLabel.Text = $"{actionName}: OK";
            AppendLog($"{DateTime.Now:HH:mm:ss} [{actionName}] {result}");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"{actionName}: FAIL";
            AppendLog($"{DateTime.Now:HH:mm:ss} [{actionName}] ERROR: {ex.Message}");
        }
    }

    void AppendLog(string line)
    {
        _log.AppendLine(line);
        SetLog(_log.ToString());
    }

    void SetLog(string text)
    {
        if (_logView.Buffer == null)
            _logView.Buffer = new TextBuffer(new TextTagTable());

        _logView.Buffer.Text = text;
    }
}
