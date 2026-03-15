using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Supertoys.PocketSphinx;

namespace joi_gtk.Services;

public sealed class PocketSphinxVoiceCommandSource : IDisposable
{
    Process _process;
    CancellationTokenSource _cts;
    Task _readerTask;

    public event Action<string, double> PhraseDetected;

    public bool IsRunning => _process != null && !_process.HasExited;

    public string Start()
    {
        if (IsRunning)
            return "Voice listener already running.";

        try
        {
            _cts = new CancellationTokenSource();
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pocketsphinx_continuous",
                    Arguments = "-inmic yes -logfn /dev/null -time yes",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            _process.Start();
            _readerTask = Task.Run(() => ReadLoop(_process, _cts.Token), _cts.Token);
            return "Voice listener started (PocketSphinx + Supertoys parser).";
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
            if (_process != null && !_process.HasExited)
                _process.Kill(true);
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

    async Task ReadLoop(Process process, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && process != null && !process.HasExited)
        {
            string line = await process.StandardOutput.ReadLineAsync();
            if (line == null)
                break;

            string hypothesis = PocketSphinxOutputParser.ExtractHypothesis(line);
            if (string.IsNullOrWhiteSpace(hypothesis))
                continue;

            double confidence = PocketSphinxOutputParser.ExtractConfidence(line);
            PhraseDetected?.Invoke(hypothesis, confidence);
        }
    }

    void Cleanup()
    {
        if (_process != null)
        {
            _process.Dispose();
            _process = null;
        }

        _cts?.Dispose();
        _cts = null;
        _readerTask = null;
    }

    public void Dispose()
    {
        Stop();
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
