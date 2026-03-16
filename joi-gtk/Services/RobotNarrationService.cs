using AeonVoice;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        _voiceProfile = Environment.GetEnvironmentVariable("ARTHUR_AEONVOICE_VOICE") ?? "Leena";
        string dataPath = FirstNonEmpty(
            Environment.GetEnvironmentVariable("ARTHUR_AEONVOICE_DATA_PATH"),
            Environment.GetEnvironmentVariable("AEONVOICE_DATA_PATH"));
        string configPath = FirstNonEmpty(
            Environment.GetEnvironmentVariable("ARTHUR_AEONVOICE_CONFIG_PATH"),
            Environment.GetEnvironmentVariable("AEONVOICE_CONFIG_PATH"));

        if (string.IsNullOrWhiteSpace(dataPath) || string.IsNullOrWhiteSpace(configPath))
        {
            Status = "Voice unavailable: set AEONVOICE_DATA_PATH and AEONVOICE_CONFIG_PATH.";
            return;
        }

        if (!Directory.Exists(dataPath) || !Directory.Exists(configPath))
        {
            Status = $"Voice unavailable: AeonVoice paths missing (data={dataPath}, config={configPath}).";
            return;
        }

        try
        {
            _engine = new AeonVoiceEngine(dataPath, configPath);
            IsAvailable = true;
            Status = $"Voice ready (provider=AeonVoice, voice={_voiceProfile}).";
        }
        catch (Exception ex)
        {
            Status = $"Voice unavailable: {ex.Message}";
        }
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

    static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
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
