using Gdk;
using Gtk;
using System;
using System.Diagnostics;
using System.IO;

namespace joi_gtk.Services;

public static class GtkWindowIconService
{
    static Pixbuf _icon;
    static bool _defaultApplied;
    static string _loadedPath = "unresolved";

    public static string LoadedIconPath => _loadedPath;

    public static void ApplyDefault()
    {
        try
        {
            _icon ??= LoadIcon();
            if (_icon == null || _defaultApplied)
                return;

            Gtk.Window.DefaultIcon = _icon;
            _defaultApplied = true;
        }
        catch
        {
            // Icon load failures must never block app startup.
        }
    }

    public static void Apply(Gtk.Window window)
    {
        if (window == null)
            return;

        try
        {
            _icon ??= LoadIcon();
            if (_icon != null)
            {
                window.Icon = _icon;
            }
        }
        catch
        {
            // Icon load failures must never block window creation.
        }

        window.Realized += (_, _) =>
        {
            try
            {
                if (_icon != null)
                    window.Icon = _icon;
            }
            catch
            {
                // Best effort.
            }
        };
    }

    static Pixbuf LoadIcon()
    {
        string runtimeResources = Path.Combine(AppContext.BaseDirectory, "resources");
        string runtimeSvg = Path.Combine(runtimeResources, "animals.svg");
        string runtimeIco = Path.Combine(runtimeResources, "animals.ico");
        string runtimePng = Path.Combine(runtimeResources, "animals.png");
        TryGeneratePngFromIco(runtimeIco, runtimePng);

        string workspaceSvg = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "animals.svg"));
        string workspaceIcoLocal = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "animals.ico"));
        string workspaceIco = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "resources", "animals.ico"));
        string workspacePng = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "resources", "animals.png"));
        TryGeneratePngFromIco(workspaceIcoLocal, Path.Combine(Path.GetDirectoryName(workspaceIcoLocal) ?? ".", "animals.png"));
        TryGeneratePngFromIco(workspaceIco, workspacePng);

        string[] candidates =
        {
            runtimeSvg,
            runtimePng,
            runtimeIco,
            workspaceSvg,
            Path.Combine(Path.GetDirectoryName(workspaceIcoLocal) ?? ".", "animals.png"),
            workspaceIcoLocal,
            workspacePng,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "resources", "animals.ico"))
        };

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                _loadedPath = path;
                return new Pixbuf(path);
            }
        }

        return null;
    }

    static void TryGeneratePngFromIco(string icoPath, string pngPath)
    {
        try
        {
            if (!File.Exists(icoPath))
                return;
            if (File.Exists(pngPath))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(pngPath) ?? ".");
            using Process process = new();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("-y");
            process.StartInfo.ArgumentList.Add("-loglevel");
            process.StartInfo.ArgumentList.Add("error");
            process.StartInfo.ArgumentList.Add("-i");
            process.StartInfo.ArgumentList.Add(icoPath);
            process.StartInfo.ArgumentList.Add(pngPath);
            process.Start();
            process.WaitForExit(3000);
        }
        catch
        {
            // Best effort only.
        }
    }
}
