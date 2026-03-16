using Gtk;
using joi_gtk.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace joi_gtk;

public sealed class CameraWindow : Window
{
    readonly ComboBoxText _deviceSelection = new();
    readonly Label _statusLabel = new("Camera idle");
    readonly Image _previewImage = new();
    readonly Button _startButton;
    readonly Button _stopButton;

    string _activeCaptureArgs;
    readonly List<string> _devicePaths = new();
    Process _cameraProcess;
    CancellationTokenSource _cameraCts;
    Task _cameraTask;

    public CameraWindow() : base("Robot Camera Feed")
    {
        SetWmclass("arthur-bipedal", "ArthurBipedal");
        GtkWindowIconService.Apply(this);
        SetDefaultSize(900, 640);
        Resizable = false;
        BorderWidth = 10;
        DeleteEvent += (_, e) =>
        {
            StopCamera();
            e.RetVal = false;
        };

        Box root = new(Orientation.Vertical, 8);
        Add(root);

        Label title = new("Live Camera Feed");
        title.Xalign = 0;
        root.PackStart(title, false, false, 0);

        Box controls = new(Orientation.Horizontal, 8);
        controls.PackStart(new Label("Device") { Xalign = 0 }, false, false, 0);
        controls.PackStart(_deviceSelection, false, false, 0);

        _startButton = CreateButton("Start", (_, _) => StartCamera());
        _stopButton = CreateButton("Stop", (_, _) => StopCamera());
        _stopButton.Sensitive = false;
        controls.PackStart(_startButton, false, false, 0);
        controls.PackStart(_stopButton, false, false, 0);
        controls.PackStart(_statusLabel, false, false, 10);
        root.PackStart(controls, false, false, 0);

        ScrolledWindow previewScroll = new()
        {
            HscrollbarPolicy = PolicyType.Automatic,
            VscrollbarPolicy = PolicyType.Automatic
        };
        previewScroll.Add(_previewImage);
        root.PackStart(previewScroll, true, true, 0);

        PopulateDevices();
    }

    static Button CreateButton(string text, EventHandler onClick)
    {
        Button button = new(text);
        button.Clicked += onClick;
        return button;
    }

    void PopulateDevices()
    {
        _deviceSelection.RemoveAll();
        _devicePaths.Clear();

        try
        {
            foreach (string path in EnumerateLinuxVideoDevices())
            {
                _devicePaths.Add(path);
                _deviceSelection.AppendText(path);
            }

            if (_devicePaths.Count > 0)
            {
                _deviceSelection.Active = 0;
                _statusLabel.Text = $"Found {_devicePaths.Count} camera device(s).";
            }
            else
            {
                _statusLabel.Text = "No camera devices found.";
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Camera discovery error: {ex.Message}";
        }
    }

    void StartCamera()
    {
        StopCamera();

        int selectedIndex = _deviceSelection.Active;
        if (selectedIndex < 0 || selectedIndex >= _devicePaths.Count)
        {
            _statusLabel.Text = "Select a camera device first.";
            return;
        }

        string devicePath = _devicePaths[selectedIndex];
        try
        {
            if (!TryResolveCaptureArgs(devicePath, out string captureArgs, out string probeError))
            {
                _statusLabel.Text = $"Camera start error: {probeError}";
                return;
            }

            _activeCaptureArgs = captureArgs;
            _cameraCts = new CancellationTokenSource();
            _cameraProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel error {_activeCaptureArgs} -f mjpeg pipe:1",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!_cameraProcess.Start())
                throw new InvalidOperationException("Unable to start ffmpeg camera process.");

            _cameraTask = Task.Run(() => PumpFrames(_cameraProcess.StandardOutput.BaseStream, _cameraCts.Token), _cameraCts.Token);
            _ = Task.Run(() => CaptureErrors(_cameraProcess.StandardError, _cameraCts.Token), _cameraCts.Token);

            _startButton.Sensitive = false;
            _stopButton.Sensitive = true;
            _statusLabel.Text = $"Camera streaming from {devicePath}.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Camera start error: {ex.Message}";
            StopCamera();
        }
    }

    void StopCamera()
    {
        if (_cameraCts != null)
        {
            try { _cameraCts.Cancel(); } catch { }
            _cameraCts.Dispose();
            _cameraCts = null;
        }

        if (_cameraProcess != null)
        {
            try
            {
                if (!_cameraProcess.HasExited)
                    _cameraProcess.Kill();
            }
            catch { }

            _cameraProcess.Dispose();
            _cameraProcess = null;
        }

        _cameraTask = null;
        _activeCaptureArgs = null;
        _startButton.Sensitive = true;
        _stopButton.Sensitive = false;
        if (_statusLabel.Text.StartsWith("Camera streaming", StringComparison.Ordinal))
            _statusLabel.Text = "Camera stopped.";
    }

    static IEnumerable<string> EnumerateLinuxVideoDevices()
    {
        if (!Directory.Exists("/dev"))
            return Array.Empty<string>();

        Regex pattern = new("^video\\d+$", RegexOptions.Compiled);
        return Directory.EnumerateFiles("/dev", "video*")
            .Where(path => pattern.IsMatch(System.IO.Path.GetFileName(path)))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    static bool TryResolveCaptureArgs(string devicePath, out string captureArgs, out string error)
    {
        string[] candidates =
        {
            $"-f video4linux2 -framerate 15 -i \"{devicePath}\"",
            $"-f video4linux2 -framerate 30 -video_size 640x480 -i \"{devicePath}\"",
            $"-f video4linux2 -input_format mjpeg -framerate 30 -video_size 640x480 -i \"{devicePath}\"",
            $"-f video4linux2 -input_format yuyv422 -framerate 30 -video_size 640x480 -i \"{devicePath}\"",
            $"-f video4linux2 -input_format yuyv422 -framerate 15 -video_size 320x240 -i \"{devicePath}\""
        };

        foreach (string args in candidates)
        {
            if (ProbeCaptureArgs(args, out string _))
            {
                captureArgs = args;
                error = string.Empty;
                return true;
            }
        }

        captureArgs = string.Empty;
        error = "no compatible V4L2 capture format found for selected device.";
        return false;
    }

    static bool ProbeCaptureArgs(string baseArgs, out string stderrLine)
    {
        stderrLine = string.Empty;
        try
        {
            using Process probe = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel error {baseArgs} -frames:v 1 -f null -",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!probe.Start())
            {
                stderrLine = "unable to launch ffmpeg.";
                return false;
            }

            stderrLine = probe.StandardError.ReadLine() ?? string.Empty;
            probe.WaitForExit(1200);
            return probe.ExitCode == 0;
        }
        catch (Exception ex)
        {
            stderrLine = ex.Message;
            return false;
        }
    }

    void PumpFrames(Stream stdout, CancellationToken token)
    {
        byte[] chunk = new byte[8192];
        List<byte> buffer = new(512 * 1024);

        while (!token.IsCancellationRequested)
        {
            int bytesRead = stdout.Read(chunk, 0, chunk.Length);
            if (bytesRead <= 0)
                break;

            for (int i = 0; i < bytesRead; i++)
                buffer.Add(chunk[i]);

            while (TryExtractJpegFrame(buffer, out byte[] frame))
            {
                Application.Invoke(delegate
                {
                    if (token.IsCancellationRequested)
                        return;

                    try
                    {
                        using MemoryStream stream = new(frame);
                        Gdk.Pixbuf next = new(stream);
                        Gdk.Pixbuf previous = _previewImage.Pixbuf;
                        _previewImage.Pixbuf = next;
                        previous?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _statusLabel.Text = $"Camera frame decode error: {ex.Message}";
                    }
                });
            }
        }

        Application.Invoke(delegate
        {
            if (!_startButton.Sensitive)
                StopCamera();
        });
    }

    async Task CaptureErrors(StreamReader stderr, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            string line = await stderr.ReadLineAsync();
            if (line == null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            Application.Invoke(delegate
            {
                if (_statusLabel.Text.StartsWith("Camera frame decode error:", StringComparison.Ordinal))
                    return;
                _statusLabel.Text = $"Camera: {line}";
            });
        }
    }

    static bool TryExtractJpegFrame(List<byte> buffer, out byte[] frame)
    {
        frame = null;
        if (buffer.Count < 4)
            return false;

        int start = -1;
        for (int i = 0; i < buffer.Count - 1; i++)
        {
            if (buffer[i] == 0xFF && buffer[i + 1] == 0xD8)
            {
                start = i;
                break;
            }
        }

        if (start < 0)
        {
            if (buffer.Count > 1)
                buffer.RemoveRange(0, buffer.Count - 1);
            return false;
        }

        int end = -1;
        for (int i = start + 2; i < buffer.Count - 1; i++)
        {
            if (buffer[i] == 0xFF && buffer[i + 1] == 0xD9)
            {
                end = i + 1;
                break;
            }
        }

        if (end < 0)
        {
            if (start > 0)
                buffer.RemoveRange(0, start);
            if (buffer.Count > 8 * 1024 * 1024)
                buffer.Clear();
            return false;
        }

        int length = end - start + 1;
        frame = buffer.GetRange(start, length).ToArray();
        buffer.RemoveRange(0, end + 1);
        return true;
    }
}
