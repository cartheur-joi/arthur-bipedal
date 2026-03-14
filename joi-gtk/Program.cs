using Cartheur.Animals.Robot;
using Gtk;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace joi_gtk;

internal static class Program
{
    public static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        ConfigureNativeResolver();
        EnsureRuntimeFolders();
        Application.Init();
        MainWindow window = new();
        window.ShowAll();
        Application.Run();
    }

    static void ConfigureNativeResolver()
    {
        NativeLibrary.SetDllImportResolver(
            typeof(Dynamixel).Assembly,
            static (libraryName, _, _) =>
            {
                if (!string.Equals(libraryName, "lib/libdxl_x64_c.so", StringComparison.Ordinal))
                    return IntPtr.Zero;

                string preferredLibrary = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                    ? "libdxl_arm64_c.so"
                    : "libdxl_x64_c.so";

                string fullPath = Path.Combine(AppContext.BaseDirectory, "lib", preferredLibrary);
                return File.Exists(fullPath) ? NativeLibrary.Load(fullPath) : IntPtr.Zero;
            });
    }

    static void EnsureRuntimeFolders()
    {
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "logs"));
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "db"));
    }
}
