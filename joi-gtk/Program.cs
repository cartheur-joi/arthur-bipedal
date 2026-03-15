using Cartheur.Animals.Robot;
using Gtk;
using joi_gtk.Services;
using System;
using System.IO;
using System.Linq;
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

        if (args.Length > 0 && string.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
        {
            RunSelfTest();
            return;
        }
        if (args.Length > 0 && string.Equals(args[0], "--probe", StringComparison.OrdinalIgnoreCase))
        {
            RunBusProbe();
            return;
        }

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

    static void RunSelfTest()
    {
        RobotControlService service = new();
        Console.WriteLine(service.Initialize());
        var snapshot = service.ReadMotorMonitoringSnapshot(MotorFunctions.PresentLoadAlarm);
        int overloads = 0;
        int commErrors = 0;
        for (int i = 0; i < snapshot.Count; i++)
        {
            var row = snapshot[i];
            if (row.Overload) overloads++;
            if (!row.CommunicationOk) commErrors++;
            Console.WriteLine($"{row.MotorName}({row.ID}) zone={row.Location} torque={(row.TorqueOn ? "ON" : "OFF")} load={row.Load} overload={row.Overload} comm={(row.CommunicationOk ? "OK" : "ERR")}");
        }
        Console.WriteLine($"SUMMARY total={snapshot.Count} overloads={overloads} comm_errors={commErrors}");
        if (snapshot.Count(r => r.Location == "upper" && !r.CommunicationOk) > 0)
            ScanUpperBusBaud();
    }

    static void ScanUpperBusBaud()
    {
        Console.WriteLine("UPPER BAUD SCAN:");
        int[] baudCandidates = { 1000000, 57600, 115200, 200000, 250000, 500000 };
        byte[] probeIds = { 31, 41, 51 };
        foreach (int baud in baudCandidates)
        {
            Dynamixel.setBaudRate(MotorFunctions.PortNumberUpper, baud);
            int responders = 0;
            foreach (byte id in probeIds)
            {
                Dynamixel.ping(MotorFunctions.PortNumberUpper, MotorFunctions.ProtocolVersion, id);
                int txRxResult = Dynamixel.getLastTxRxResult(MotorFunctions.PortNumberUpper, MotorFunctions.ProtocolVersion);
                byte packetError = Dynamixel.getLastRxPacketError(MotorFunctions.PortNumberUpper, MotorFunctions.ProtocolVersion);
                if (txRxResult == MotorFunctions.ComSuccess && packetError == 0)
                    responders++;
            }

            Console.WriteLine($"  baud={baud} responders={responders}/{probeIds.Length}");
        }
    }

    static void RunBusProbe()
    {
        string upperPort = Environment.GetEnvironmentVariable("ARTHUR_UPPER_PORT") ?? "/dev/ttyUSB1";
        string lowerPort = Environment.GetEnvironmentVariable("ARTHUR_LOWER_PORT") ?? "/dev/ttyUSB0";
        int upper = Dynamixel.portHandler(upperPort);
        int lower = Dynamixel.portHandler(lowerPort);
        Dynamixel.packetHandler();

        bool upperOpened = Dynamixel.openPort(upper);
        bool lowerOpened = Dynamixel.openPort(lower);
        Console.WriteLine($"PORTS upper={upperPort} open={upperOpened} lower={lowerPort} open={lowerOpened}");

        int[] baudCandidates = { 1000000, 57600, 115200, 200000, 250000, 500000 };
        byte[] lowerIds = { 11, 21, 25 };
        byte[] upperIds = { 31, 41, 51 };

        foreach (int baud in baudCandidates)
        {
            int lowerHits = 0;
            int upperHits = 0;

            if (lowerOpened)
            {
                Dynamixel.setBaudRate(lower, baud);
                foreach (byte id in lowerIds)
                {
                    Dynamixel.ping(lower, MotorFunctions.ProtocolVersion, id);
                    int txRxResult = Dynamixel.getLastTxRxResult(lower, MotorFunctions.ProtocolVersion);
                    byte packetError = Dynamixel.getLastRxPacketError(lower, MotorFunctions.ProtocolVersion);
                    if (txRxResult == MotorFunctions.ComSuccess && packetError == 0)
                        lowerHits++;
                }
            }

            if (upperOpened)
            {
                Dynamixel.setBaudRate(upper, baud);
                foreach (byte id in upperIds)
                {
                    Dynamixel.ping(upper, MotorFunctions.ProtocolVersion, id);
                    int txRxResult = Dynamixel.getLastTxRxResult(upper, MotorFunctions.ProtocolVersion);
                    byte packetError = Dynamixel.getLastRxPacketError(upper, MotorFunctions.ProtocolVersion);
                    if (txRxResult == MotorFunctions.ComSuccess && packetError == 0)
                        upperHits++;
                }
            }

            Console.WriteLine($"baud={baud} lower_hits={lowerHits}/{lowerIds.Length} upper_hits={upperHits}/{upperIds.Length}");
        }

        if (upperOpened) Dynamixel.closePort(upper);
        if (lowerOpened) Dynamixel.closePort(lower);
    }
}
