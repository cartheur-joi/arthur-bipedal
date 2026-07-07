using System.Runtime.InteropServices;
using Gtk;

namespace joi_gtk.Services;

public static class GtkProcessIdentityService
{
    const string Glib = "libglib-2.0.so.0";

    [DllImport(Glib, CallingConvention = CallingConvention.Cdecl)]
    static extern void g_set_prgname([MarshalAs(UnmanagedType.LPUTF8Str)] string prgname);

    [DllImport(Glib, CallingConvention = CallingConvention.Cdecl)]
    static extern void g_set_application_name([MarshalAs(UnmanagedType.LPUTF8Str)] string applicationName);

    public static void Apply()
    {
        try
        {
            g_set_prgname("arthur-bipedal");
            g_set_application_name("Arthur Bipedal");
        }
        catch
        {
            // Identity hints are best-effort only.
        }
    }

    public static void ApplyWindowClass(Window window)
    {
        if (window == null)
            return;

        try
        {
#pragma warning disable CS0612
            window.SetWmclass("arthur-bipedal", "ArthurBipedal");
#pragma warning restore CS0612
        }
        catch
        {
            // Best effort only.
        }
    }
}
