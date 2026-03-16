using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace joi_gtk.Services;

public sealed class PocketSphinxVoiceCommandSource : IDisposable
{
    readonly RobotSpeechRecognitionService _speech = new();
    CancellationTokenSource _cts;
    Task _listenTask;

    public event Action<string, double> PhraseDetected;
    public event Action ListenWindowStarted;
    public event Action ListenWindowEnded;

    public bool IsRunning => _listenTask != null && !_listenTask.IsCompleted;

    public string Start()
    {
        if (IsRunning)
            return "Voice listener already running.";
        if (!_speech.IsAvailable)
            return $"Voice listener unavailable: {_speech.Status}";

        try
        {
            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoop(_cts.Token), _cts.Token);
            return "Voice listener started (PocketSphinx 5-second windows, commands: START/STOP).";
        }
        catch (Exception ex)
        {
            Cleanup();
            return $"Voice listener unavailable: {ex.Message}";
        }
    }

    public string Stop()
    {
        if (!IsRunning)
            return "Voice listener is not running.";

        try
        {
            _cts?.Cancel();
        }
        catch
        {
            // Best-effort stop.
        }
        finally
        {
            Cleanup();
        }

        return "Voice listener stopped.";
    }

    async Task ListenLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string wavPath = Path.Combine(Path.GetTempPath(), $"arthur-voice-window-{Guid.NewGuid():N}.wav");
            try
            {
                ListenWindowStarted?.Invoke();
                await RecordWindowAsync(wavPath, seconds: 5, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Keep listener loop resilient to transient capture errors.
            }
            finally
            {
                ListenWindowEnded?.Invoke();
            }

            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                SpeechRecognitionRunResult result = await _speech.RecognizeFileAsync(wavPath, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(result.Hypothesis))
                    PhraseDetected?.Invoke(result.Hypothesis, result.Confidence);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Best effort: ignore recognition failures and continue loop.
            }
            finally
            {
                try
                {
                    if (File.Exists(wavPath))
                        File.Delete(wavPath);
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }

            try
            {
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    static async Task RecordWindowAsync(string wavPath, int seconds, CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "arecord",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("-q");
        process.StartInfo.ArgumentList.Add("-d");
        process.StartInfo.ArgumentList.Add(seconds.ToString());
        process.StartInfo.ArgumentList.Add("-f");
        process.StartInfo.ArgumentList.Add("S16_LE");
        process.StartInfo.ArgumentList.Add("-r");
        process.StartInfo.ArgumentList.Add("16000");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add("1");
        process.StartInfo.ArgumentList.Add(wavPath);

        process.Start();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }

    void Cleanup()
    {
        _cts?.Dispose();
        _cts = null;
        _listenTask = null;
    }

    public void Dispose()
    {
        Stop();
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
