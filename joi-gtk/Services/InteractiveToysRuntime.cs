using System;
using System.Threading;

namespace joi_gtk.Services;

public sealed class InteractiveToysRuntime : IDisposable
{
    readonly IPersonalityRuntime _personality;
    readonly IRobotNarrationService _narration;
    readonly object _consoleGate = new();
    bool _disposed;

    public InteractiveToysRuntime()
    {
        _personality = PersonalityRuntimeFactory.Create();
        _narration = new RobotNarrationService();
    }

    public string Status =>
        $"interactive-ready personality={_personality.Name} personality_enabled={_personality.IsEnabled} speech_available={_narration.IsAvailable}";

    public void RunTextLoop(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("INTERACTIVE_TOYS mode=text");
        Console.WriteLine($"INTERACTIVE_TOYS status={Status}");
        Console.WriteLine("INTERACTIVE_TOYS commands: /exit, /quit, /status");

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("you> ");
            string input = Console.ReadLine() ?? "/exit";

            string trimmed = input.Trim();
            if (trimmed.Length == 0)
                continue;

            if (string.Equals(trimmed, "/exit", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "/quit", StringComparison.OrdinalIgnoreCase))
                break;

            if (string.Equals(trimmed, "/status", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"system> {Status}");
                continue;
            }

            string baseReply = BuildBaseReply(trimmed);
            string adapted = _personality.AdaptSpeech(baseReply, trimmed);
            Console.WriteLine($"arthur> {adapted}");
            if (_narration.IsAvailable)
                _narration.Announce(adapted);
        }
    }

    public string RunSingleTurn(string userText)
    {
        string baseReply = BuildBaseReply(userText ?? string.Empty);
        string adapted = _personality.AdaptSpeech(baseReply, userText ?? string.Empty);
        return adapted;
    }

    public void RunVoiceLoop(CancellationToken cancellationToken = default)
    {
        using PocketSphinxVoiceCommandSource source = new();
        Console.WriteLine("INTERACTIVE_TOYS mode=voice");
        Console.WriteLine($"INTERACTIVE_TOYS status={Status}");

        bool stopRequested = false;
        source.ListenWindowStarted += () => WriteSystem("listening...");
        source.ListenWindowEnded += () => WriteSystem("processing...");
        source.PhraseDetected += (phrase, confidence) =>
        {
            string recognized = (phrase ?? string.Empty).Trim();
            if (recognized.Length == 0)
                return;

            lock (_consoleGate)
            {
                Console.WriteLine($"you(voice)> {recognized} (confidence={confidence:F3})");
            }

            if (IsVoiceExitPhrase(recognized))
            {
                string goodbye = _personality.AdaptSpeech("Ending interactive voice mode now.", recognized);
                WriteArthur(goodbye);
                stopRequested = true;
                return;
            }

            string adapted = RunSingleTurn(recognized);
            WriteArthur(adapted);
        };

        string startup = source.Start();
        Console.WriteLine($"INTERACTIVE_TOYS voice_listener={startup}");
        if (!source.IsRunning)
            return;

        while (!cancellationToken.IsCancellationRequested && !stopRequested)
            Thread.Sleep(150);

        _ = source.Stop();
        Console.WriteLine("INTERACTIVE_TOYS voice_session=stopped");
    }

    static string BuildBaseReply(string userText)
    {
        string normalized = (userText ?? string.Empty).Trim();
        if (normalized.Length == 0)
            return "I am listening.";

        if (normalized.EndsWith("?", StringComparison.Ordinal))
            return "I heard your question. I will think with you.";

        return $"I heard: {normalized}";
    }

    static bool IsVoiceExitPhrase(string phrase)
    {
        string normalized = (phrase ?? string.Empty).Trim().ToLowerInvariant();
        return normalized.Contains("stop listening", StringComparison.Ordinal) ||
               normalized.Contains("exit interactive", StringComparison.Ordinal) ||
               normalized == "stop" ||
               normalized == "exit" ||
               normalized == "quit";
    }

    void WriteArthur(string adapted)
    {
        lock (_consoleGate)
        {
            Console.WriteLine($"arthur> {adapted}");
        }

        if (_narration.IsAvailable)
            _narration.Announce(adapted);
    }

    void WriteSystem(string line)
    {
        lock (_consoleGate)
        {
            Console.WriteLine($"system> {line}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_narration is IDisposable disposable)
            disposable.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
