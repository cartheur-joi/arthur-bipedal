using System;
using System.Diagnostics;
using System.IO;

namespace joi_gtk.Services;

public static class AudioCueService
{
    public static void PlayTone(int frequencyHz, int durationMs, int repeats = 1, int gapMs = 60)
    {
        for (int i = 0; i < Math.Max(1, repeats); i++)
        {
            try
            {
                string wavPath = Path.Combine(Path.GetTempPath(), $"arthur-tone-{Guid.NewGuid():N}.wav");
                WriteToneWave(wavPath, frequencyHz, durationMs);
                PlayWaveFile(wavPath);
                TryDelete(wavPath);
            }
            catch
            {
                // Tone feedback must never break control flow.
            }

            if (i < repeats - 1 && gapMs > 0)
                System.Threading.Thread.Sleep(gapMs);
        }
    }

    static void PlayWaveFile(string path)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "aplay",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("-q");
        process.StartInfo.ArgumentList.Add(path);
        process.Start();
        process.WaitForExit(5_000);
    }

    static void WriteToneWave(string path, int frequencyHz, int durationMs)
    {
        const int sampleRate = 16000;
        int sampleCount = Math.Max(1, sampleRate * durationMs / 1000);
        short[] samples = new short[sampleCount];
        double omega = 2.0 * Math.PI * Math.Max(80, frequencyHz) / sampleRate;
        for (int i = 0; i < sampleCount; i++)
        {
            double envelope = i < 80 ? i / 80.0 : 1.0;
            samples[i] = (short)(Math.Sin(i * omega) * envelope * 12000);
        }

        int byteRate = sampleRate * 2;
        int dataLength = samples.Length * 2;

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using BinaryWriter writer = new(stream);
        writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        writer.Write(36 + dataLength);
        writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
        writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        writer.Write(dataLength);
        foreach (short s in samples)
            writer.Write(s);
    }

    static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }
}
