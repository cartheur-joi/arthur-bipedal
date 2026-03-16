using Gdk;
using Gtk;
using System;
using System.IO;

namespace joi_gtk.Services;

public static class GtkWindowIconService
{
    static Pixbuf _icon;

    public static void Apply(Gtk.Window window)
    {
        if (window == null)
            return;

        try
        {
            _icon ??= LoadIcon();
            if (_icon != null)
                window.Icon = _icon;
        }
        catch
        {
            // Icon load failures must never block window creation.
        }
    }

    static Pixbuf LoadIcon()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "resources", "animals.ico"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "animals.ico"))
        };

        foreach (string path in candidates)
        {
            if (File.Exists(path))
                return new Pixbuf(path);
        }

        return null;
    }
}
