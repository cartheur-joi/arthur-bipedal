using Cartheur.Animals.Robot;
using Gtk;
using joi_gtk.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

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
        if (args.Length > 0 && string.Equals(args[0], "--ankle-sweep", StringComparison.OrdinalIgnoreCase))
        {
            RunAnkleSweepTest();
            return;
        }
        if (args.Length > 1 && string.Equals(args[0], "--sweep", StringComparison.OrdinalIgnoreCase))
        {
            RunSingleMotorSweep(args[1]);
            return;
        }
        if (args.Length > 1 && string.Equals(args[0], "--sweep-group", StringComparison.OrdinalIgnoreCase))
        {
            RunSweepGroup(args[1]);
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

    static void RunAnkleSweepTest()
    {
        RunSingleMotorSweep("r_ankle_y", "ANKLE");
    }

    static void RunSingleMotorSweep(string motor, string label = "SWEEP")
    {
        RobotControlService service = new();
        Console.WriteLine(service.Initialize());

        byte id = Motor.ReturnID(motor);
        if (id == 0)
            throw new InvalidOperationException($"Unknown motor '{motor}'.");
        string location = Motor.ReturnLocation(motor);
        int port = location == "upper" ? MotorFunctions.PortNumberUpper : MotorFunctions.PortNumberLower;
        MotorFunctions.SetBaudRate(location);

        ushort cwLimit = Dynamixel.read2ByteTxRx(port, MotorFunctions.ProtocolVersion, id, 6);
        ushort ccwLimit = Dynamixel.read2ByteTxRx(port, MotorFunctions.ProtocolVersion, id, 8);
        int current = service.ReadPositions(new[] { motor })[motor];
        byte torqueBefore = Dynamixel.read1ByteTxRx(port, MotorFunctions.ProtocolVersion, id, (ushort)MotorFunctions.MxAddress);

        Console.WriteLine($"{label}_SETUP motor={motor} id={id} zone={location} cw={cwLimit} ccw={ccwLimit} current={current} torque_before={(torqueBefore == 1 ? "ON" : "OFF")}");

        service.SetTorqueOn(new[] { motor });
        Thread.Sleep(100);

        // Keep sweep local around the currently observed joint position to avoid hard-stop loading.
        int margin = 50;
        int safeSpanFromCurrent = 120;
        int low = Math.Max(Math.Max(cwLimit + margin, 0), current - safeSpanFromCurrent);
        int high = Math.Min(Math.Min(ccwLimit - margin, 4095), current + safeSpanFromCurrent);
        if (high - low < 40)
        {
            low = Math.Max(Math.Max(cwLimit + 10, 0), current - 20);
            high = Math.Min(Math.Min(ccwLimit - 10, 4095), current + 20);
        }
        if (high <= low)
            throw new InvalidOperationException($"Invalid ankle sweep range: low={low}, high={high}, cw={cwLimit}, ccw={ccwLimit}, current={current}");

        int mid = low + (high - low) / 2;
        int[] targets = new[] { low, mid, high, mid, low };
        Console.WriteLine($"{label}_TARGETS {string.Join(", ", targets)}");

        for (int i = 0; i < targets.Length; i++)
        {
            int target = targets[i];
            service.MoveToPositions(new Dictionary<string, int> { [motor] = target }, 1800, 10);

            List<int> samples = new();
            for (int s = 0; s < 8; s++)
            {
                Thread.Sleep(200);
                samples.Add(service.ReadPositions(new[] { motor })[motor]);
            }

            Console.WriteLine($"{label}_STEP {i + 1} motor={motor} target={target} metrical={string.Join(" ", samples)}");
        }

        // Safety reset: always return to the original observed position.
        service.MoveToPositions(new Dictionary<string, int> { [motor] = current }, 1800, 10);
        List<int> returnSamples = new();
        for (int s = 0; s < 8; s++)
        {
            Thread.Sleep(200);
            returnSamples.Add(service.ReadPositions(new[] { motor })[motor]);
        }
        Console.WriteLine($"{label}_RETURN motor={motor} target={current} metrical={string.Join(" ", returnSamples)}");

        if (torqueBefore != MotorFunctions.TorqueEnable)
            service.SetTorqueOff(new[] { motor });

        Console.WriteLine($"{label}_COMPLETE motor={motor}");
    }

    static void RunSweepGroup(string groupName)
    {
        Dictionary<string, string[]> groups = new(StringComparer.OrdinalIgnoreCase)
        {
            ["left-arm"] = Limbic.LeftArm,
            ["right-arm"] = Limbic.RightArm,
            ["head"] = Limbic.Head,
            ["abdomen"] = Limbic.Abdomen,
            ["left-leg"] = Limbic.LeftLeg,
            ["right-leg"] = Limbic.RightLeg
        };

        if (!groups.TryGetValue(groupName, out string[] motors))
            throw new InvalidOperationException($"Unknown group '{groupName}'. Valid groups: {string.Join(", ", groups.Keys)}");

        foreach (string motor in motors)
        {
            try
            {
                RunSingleMotorSweep(motor, "GROUP_SWEEP");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GROUP_SWEEP_ERROR motor={motor} error={ex.Message}");
            }
        }
    }
}
