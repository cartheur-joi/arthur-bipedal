using System.Runtime.InteropServices;

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
}
