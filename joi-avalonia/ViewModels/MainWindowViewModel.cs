using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using joi_avalonia.Services;
using System;
using System.Text;

namespace joi_avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    readonly RobotControlService _robot;
    readonly StringBuilder _log;

    [ObservableProperty]
    string status = "Ready";

    [ObservableProperty]
    string logText = "Avalonia robot panel initialized.";

    [ObservableProperty]
    string cyclesText = "1";

    [ObservableProperty]
    string stepDurationMsText = "800";

    [ObservableProperty]
    string interpolationStepsText = "12";

    [ObservableProperty]
    string timeoutMsText = "8000";

    [ObservableProperty]
    bool requireSupportFootContact;

    public MainWindowViewModel()
    {
        _robot = new RobotControlService();
        _log = new StringBuilder(LogText);
    }

    [RelayCommand]
    void InitializeRobot()
    {
        RunAction("Initialize", () => _robot.Initialize());
    }

    [RelayCommand]
    void TorqueOnLower()
    {
        RunAction("TorqueOnLower", () => _robot.TorqueOnLower());
    }

    [RelayCommand]
    void TorqueOffLower()
    {
        RunAction("TorqueOffLower", () => _robot.TorqueOffLower());
    }

    [RelayCommand]
    void ReadLowerTelemetry()
    {
        RunAction("ReadLowerTelemetry", () => _robot.ReadLowerTelemetry());
    }

    [RelayCommand]
    void ExecuteSupervisedWalk()
    {
        if (!TryReadWalkInputs(out int cycles, out int stepDurationMs, out int interpolationSteps, out int timeoutMs))
            return;

        RunAction(
            "ExecuteSupervisedWalk",
            () => _robot.ExecuteWalkCycleSupervised(cycles, stepDurationMs, interpolationSteps, timeoutMs, RequireSupportFootContact));
    }

    [RelayCommand]
    void ExecuteThreeCycleWalk()
    {
        CyclesText = "3";
        ExecuteSupervisedWalk();
    }

    [RelayCommand]
    void EmergencyStop()
    {
        RunAction("EmergencyStop", () => _robot.EmergencyStopLower());
    }

    bool TryReadWalkInputs(out int cycles, out int stepDurationMs, out int interpolationSteps, out int timeoutMs)
    {
        cycles = 0;
        stepDurationMs = 0;
        interpolationSteps = 0;
        timeoutMs = 0;

        if (!int.TryParse(CyclesText, out cycles) || cycles < 1)
        {
            AppendLog($"{DateTime.Now:HH:mm:ss} [Validation] Invalid cycles value.");
            Status = "Validation: FAIL";
            return false;
        }
        if (!int.TryParse(StepDurationMsText, out stepDurationMs) || stepDurationMs < 100)
        {
            AppendLog($"{DateTime.Now:HH:mm:ss} [Validation] Invalid step duration (min 100 ms).");
            Status = "Validation: FAIL";
            return false;
        }
        if (!int.TryParse(InterpolationStepsText, out interpolationSteps) || interpolationSteps < 1)
        {
            AppendLog($"{DateTime.Now:HH:mm:ss} [Validation] Invalid interpolation step count.");
            Status = "Validation: FAIL";
            return false;
        }
        if (!int.TryParse(TimeoutMsText, out timeoutMs) || timeoutMs < 1000)
        {
            AppendLog($"{DateTime.Now:HH:mm:ss} [Validation] Invalid timeout (min 1000 ms).");
            Status = "Validation: FAIL";
            return false;
        }
        return true;
    }

    void RunAction(string actionName, Func<string> action)
    {
        try
        {
            string result = action();
            Status = $"{actionName}: OK";
            AppendLog($"{DateTime.Now:HH:mm:ss} [{actionName}] {result}");
        }
        catch (Exception ex)
        {
            Status = $"{actionName}: FAIL";
            AppendLog($"{DateTime.Now:HH:mm:ss} [{actionName}] ERROR: {ex.Message}");
        }
    }

    void AppendLog(string line)
    {
        _log.AppendLine(line);
        LogText = _log.ToString();
    }
}
