using Supertoys.PocketSphinx;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace joi_gtk.Services;

public interface IRobotSpeechRecognitionService
{
    bool IsAvailable { get; }
    string Status { get; }
    string RuntimeIdentifier { get; }
    string ExecutablePath { get; }
    string ModelsDirectory { get; }
    Task<SpeechRecognitionRunResult> RecognizeFileAsync(string audioPath, CancellationToken cancellationToken = default);
}

public sealed record SpeechRecognitionRunResult(
    bool Success,
    string Hypothesis,
    double Confidence,
    int ExitCode,
    string StandardError,
    string StandardOutput);

public sealed class RobotSpeechRecognitionService : IRobotSpeechRecognitionService
{
    readonly string _acousticModelPath;
    readonly string _dictionaryPath;
    readonly string _languageModelPath;

    public RobotSpeechRecognitionService()
    {
        if (!IsRecognitionEnabled())
        {
            Status = "Speech recognition disabled (ARTHUR_SPEECH_RECOGNITION_ENABLED=0).";
            return;
        }

        RuntimeIdentifier = Environment.GetEnvironmentVariable("ARTHUR_POCKETSPHINX_RID") ??
                            PocketSphinxRuntimePaths.GetDefaultRuntimeIdentifier();
        ExecutablePath = Path.Combine(AppContext.BaseDirectory, "runtimes", RuntimeIdentifier, "native", "pocketsphinx");
        ModelsDirectory = Path.Combine(AppContext.BaseDirectory, "models", "en-us");
        _acousticModelPath = Path.Combine(ModelsDirectory, "en-us");
        _dictionaryPath = Path.Combine(ModelsDirectory, "cmudict-en-us.dict");
        _languageModelPath = Path.Combine(ModelsDirectory, "en-us.lm.bin");

        if (!File.Exists(ExecutablePath))
        {
            Status = $"Speech recognition unavailable: missing executable at {ExecutablePath}.";
            return;
        }
        if (!Directory.Exists(_acousticModelPath))
        {
            Status = $"Speech recognition unavailable: missing acoustic model directory at {_acousticModelPath}.";
            return;
        }
        if (!File.Exists(_dictionaryPath))
        {
            Status = $"Speech recognition unavailable: missing dictionary at {_dictionaryPath}.";
            return;
        }
        if (!File.Exists(_languageModelPath))
        {
            Status = $"Speech recognition unavailable: missing language model at {_languageModelPath}.";
            return;
        }

        IsAvailable = true;
        Status =
            $"Speech recognition ready (provider=Supertoys.PocketSphinx, rid={RuntimeIdentifier}, " +
            $"mode=repo-scoped, executable={ExecutablePath}, models={ModelsDirectory}).";
    }

    public bool IsAvailable { get; }
    public string Status { get; } = "Speech recognition unavailable.";
    public string RuntimeIdentifier { get; } = string.Empty;
    public string ExecutablePath { get; } = string.Empty;
    public string ModelsDirectory { get; } = string.Empty;

    public async Task<SpeechRecognitionRunResult> RecognizeFileAsync(string audioPath, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return new SpeechRecognitionRunResult(false, string.Empty, -1, -1, "Recognizer unavailable.", string.Empty);

        if (string.IsNullOrWhiteSpace(audioPath))
            return new SpeechRecognitionRunResult(false, string.Empty, -1, -1, "Audio path is empty.", string.Empty);

        string fullAudioPath = Path.GetFullPath(audioPath);
        if (!File.Exists(fullAudioPath))
            return new SpeechRecognitionRunResult(false, string.Empty, -1, -1, $"Audio file not found: {fullAudioPath}", string.Empty);

        PocketSphinxRunnerOptions options = new()
        {
            InputPath = fullAudioPath,
            RuntimeIdentifier = RuntimeIdentifier,
            ExecutablePath = ExecutablePath,
            AcousticModelPath = _acousticModelPath,
            DictionaryPath = _dictionaryPath,
            LanguageModelPath = _languageModelPath,
            Mode = "single",
            ThrowOnNonZeroExit = false
        };

        PocketSphinxRunnerResult result = await PocketSphinxRunner.RecognizeFileAsync(options, cancellationToken).ConfigureAwait(false);
        bool success = result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Hypothesis);
        return new SpeechRecognitionRunResult(
            success,
            result.Hypothesis ?? string.Empty,
            result.Confidence,
            result.ExitCode,
            result.StandardError ?? string.Empty,
            result.StandardOutput ?? string.Empty);
    }

    static bool IsRecognitionEnabled()
    {
        string raw = Environment.GetEnvironmentVariable("ARTHUR_SPEECH_RECOGNITION_ENABLED") ?? string.Empty;
        if (string.Equals(raw, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "off", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }
}
