using Cartheur.Animals.Robot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace joi_gtk.Services;

public sealed class FullSelfCheckService
{
    public string Run()
    {
        List<string> lines = new();
        int warnings = 0;
        int failures = 0;

        lines.Add($"FULL_SELF_CHECK started_utc={DateTime.UtcNow:O}");

        // Motor stack probe: initialize and read a monitoring snapshot.
        try
        {
            RobotControlService robot = new();
            string init = robot.Initialize();
            IReadOnlyList<MotorMonitorReading> snapshot = robot.ReadMotorMonitoringSnapshot(MotorFunctions.PresentLoadAlarm);
            int commErrors = snapshot.Count(r => !r.CommunicationOk);
            lines.Add($"MOTORS init={Sanitize(init)} total={snapshot.Count} comm_errors={commErrors}");
            if (snapshot.Count == 0)
            {
                warnings++;
                lines.Add("MOTORS status=WARN reason=no-motors-detected");
            }
        }
        catch (Exception ex)
        {
            failures++;
            lines.Add($"MOTORS status=FAIL error={Sanitize(ex.Message)}");
        }

        try
        {
            IPersonalityRuntime personality = PersonalityRuntimeFactory.Create();
            string input = "Initialize completed.";
            string output = personality.AdaptSpeech(input, input);
            lines.Add($"PERSONALITY name={personality.Name} enabled={personality.IsEnabled} status={Sanitize(personality.Status)}");
            lines.Add($"PERSONALITY sample_in={Sanitize(input)} sample_out={Sanitize(output)}");
            if (string.IsNullOrWhiteSpace(output))
            {
                failures++;
                lines.Add("PERSONALITY status=FAIL reason=empty-output");
            }
        }
        catch (Exception ex)
        {
            failures++;
            lines.Add($"PERSONALITY status=FAIL error={Sanitize(ex.Message)}");
        }

        try
        {
            using RobotNarrationService narration = new();
            lines.Add($"VOICE available={narration.IsAvailable} status={Sanitize(narration.Status)}");
            if (!narration.IsAvailable)
            {
                warnings++;
                lines.Add("VOICE status=WARN reason=unavailable");
            }
        }
        catch (Exception ex)
        {
            failures++;
            lines.Add($"VOICE status=FAIL error={Sanitize(ex.Message)}");
        }

        try
        {
            RobotSpeechRecognitionService speech = new();
            lines.Add($"SPEECH_RECOG available={speech.IsAvailable} rid={speech.RuntimeIdentifier} executable={speech.ExecutablePath}");
            lines.Add($"SPEECH_RECOG status={Sanitize(speech.Status)}");
            if (!speech.IsAvailable)
            {
                warnings++;
                lines.Add("SPEECH_RECOG status=WARN reason=unavailable");
            }
        }
        catch (Exception ex)
        {
            failures++;
            lines.Add($"SPEECH_RECOG status=FAIL error={Sanitize(ex.Message)}");
        }

        try
        {
            using InteractiveToysRuntime runtime = new();
            string toysOutput = runtime.RunSingleTurn("hello robot");
            lines.Add($"INTERACTIVE_TOYS status={Sanitize(runtime.Status)}");
            lines.Add($"INTERACTIVE_TOYS sample_out={Sanitize(toysOutput)}");
            if (string.IsNullOrWhiteSpace(toysOutput))
            {
                failures++;
                lines.Add("INTERACTIVE_TOYS status=FAIL reason=empty-output");
            }
        }
        catch (Exception ex)
        {
            failures++;
            lines.Add($"INTERACTIVE_TOYS status=FAIL error={Sanitize(ex.Message)}");
        }

        string result = failures == 0 ? "PASS" : "FAIL";
        lines.Add($"FULL_SELF_CHECK result={result} warnings={warnings} failures={failures}");
        return string.Join(Environment.NewLine, lines);
    }

    static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return value.Replace('\n', ' ').Replace('\r', ' ').Trim();
    }
}
