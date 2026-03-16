using AeonVoice;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace joi_gtk.Services;

public interface IRobotNarrationService
{
    bool IsAvailable { get; }
    string Status { get; }
    void Announce(string text);
}

public sealed class RobotNarrationService : IRobotNarrationService, IDisposable
{
    readonly object _speakGate = new();
    readonly AeonVoiceEngine _engine;
    readonly string _voiceProfile;

    public RobotNarrationService()
    {
        if (!IsSpeechEnabled())
        {
            Status = "Voice disabled (ARTHUR_SPEECH_ENABLED=0).";
            return;
        }

        PreloadBundledNativeLibraries();

        _voiceProfile = Environment.GetEnvironmentVariable("ARTHUR_AEONVOICE_VOICE") ?? "Leena";
        if (TryCreateAutoDetectedEngine(out AeonVoiceEngine autoEngine, out string autoStatus))
        {
            _engine = autoEngine;
            IsAvailable = true;
            Status = autoStatus;
            return;
        }

        string bundledDataPath = Path.Combine(AppContext.BaseDirectory, "aeonvoice", "data");
        string bundledConfigPath = Path.Combine(AppContext.BaseDirectory, "aeonvoice", "config");
        if (Directory.Exists(bundledDataPath) && Directory.Exists(bundledConfigPath))
        {
            try
            {
                _engine = new AeonVoiceEngine(bundledDataPath, bundledConfigPath);
                IsAvailable = true;
                Status =
                    $"Voice ready (provider=AeonVoice, mode=bundled-paths, voice={_voiceProfile}, " +
                    $"data={bundledDataPath}, config={bundledConfigPath}).";
                return;
            }
            catch (Exception bundledEx)
            {
                autoStatus += $" | bundled-paths failed: {bundledEx.Message}";
            }
        }

        Status =
            $"Voice unavailable: {autoStatus}. " +
            "Only repo-scoped packaged resources were attempted (auto-detect + bundled output paths).";
    }

    public bool IsAvailable { get; }
    public string Status { get; } = "Voice unavailable.";

    public void Announce(string text)
    {
        if (!IsAvailable || _engine == null)
            return;

        string normalized = NormalizeSpeechText(text);
        if (normalized.Length == 0)
            return;

        lock (_speakGate)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"arthur-speech-{Guid.NewGuid():N}.wav");
            try
            {
                SynthesisResult result = _engine.SynthesizeToPcm16(normalized, _voiceProfile);
                if (result.Samples == null || result.Samples.Length == 0)
                    return;

                WriteWavPcm16Mono(tempPath, result.SampleRate, result.Samples);
                TryPlayWave(tempPath);
            }
            catch
            {
                // Voice output must never break control flow.
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // Best effort cleanup.
                }
            }
        }
    }

    static bool IsSpeechEnabled()
    {
        string raw = Environment.GetEnvironmentVariable("ARTHUR_SPEECH_ENABLED") ?? string.Empty;
        if (string.Equals(raw, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "off", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    static void PreloadBundledNativeLibraries()
    {
        try
        {
            string rid = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => "linux-arm64",
                Architecture.X64 => "linux-x64",
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(rid))
                return;

            string baseDir = AppContext.BaseDirectory;
            string runtimeNativeDir = Path.Combine(baseDir, "runtimes", rid, "native");
            string[] candidateDirs = new[] { runtimeNativeDir, baseDir };
            string[] loadOrder =
            {
                "libAeonVoice_core.so.10.1.0",
                "libAeonVoice_core.so.10",
                "libAeonVoice_core.so",
                "libAeonVoice_audio.so.2.0.0",
                "libAeonVoice_audio.so.2",
                "libAeonVoice_audio.so",
                "libAeonVoice.so.5.2.0",
                "libAeonVoice.so.5",
                "libAeonVoice.so"
            };

            foreach (string dir in candidateDirs)
            {
                if (!Directory.Exists(dir))
                    continue;

                foreach (string fileName in loadOrder)
                {
                    string fullPath = Path.Combine(dir, fileName);
                    if (!File.Exists(fullPath))
                        continue;

                    NativeLibrary.TryLoad(fullPath, out _);
                }
            }
        }
        catch
        {
            // Best effort only; speech must never interrupt control flow.
        }
    }

    bool TryCreateAutoDetectedEngine(out AeonVoiceEngine engine, out string status)
    {
        engine = null;
        status = "Voice auto-detect unavailable.";
        try
        {
            engine = new AeonVoiceEngine();
            string baseDir = AppContext.BaseDirectory;
            string dataPath = Path.Combine(baseDir, "aeonvoice", "data");
            string configPath = Path.Combine(baseDir, "aeonvoice", "config");
            status =
                $"Voice ready (provider=AeonVoice, mode=auto-detect, voice={_voiceProfile}, " +
                $"data={(Directory.Exists(dataPath) ? dataPath : "n/a")}, " +
                $"config={(Directory.Exists(configPath) ? configPath : "n/a")}).";
            return true;
        }
        catch (Exception ex)
        {
            status = $"Voice auto-detect failed: {ex.Message}";
            try
            {
                engine?.Dispose();
            }
            catch
            {
                // Ignore
            }
            engine = null;
            return false;
        }
    }

    static string NormalizeSpeechText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string normalized = text.Trim().Replace('\n', ' ').Replace('\r', ' ');
        if (normalized.Length > 220)
            normalized = normalized.Substring(0, 220);
        return normalized;
    }

    static void WriteWavPcm16Mono(string path, int sampleRate, short[] samples)
    {
        int byteRate = sampleRate * 2;
        int dataLength = samples.Length * 2;

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using BinaryWriter writer = new(stream, Encoding.ASCII);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);             // Subchunk1Size for PCM
        writer.Write((short)1);       // AudioFormat PCM
        writer.Write((short)1);       // NumChannels mono
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)2);       // BlockAlign
        writer.Write((short)16);      // BitsPerSample
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);
        for (int i = 0; i < samples.Length; i++)
            writer.Write(samples[i]);
    }

    static void TryPlayWave(string wavPath)
    {
        using Process process = new();
        process.StartInfo.FileName = "aplay";
        process.StartInfo.ArgumentList.Add("-q");
        process.StartInfo.ArgumentList.Add(wavPath);
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit(15_000);
    }

    public void Dispose()
    {
        _engine?.Dispose();
    }
}
