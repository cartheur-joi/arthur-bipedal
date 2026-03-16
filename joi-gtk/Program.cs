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
        if (args.Length > 0 && string.Equals(args[0], "--imu-probe", StringComparison.OrdinalIgnoreCase))
        {
            RunImuProbe();
            return;
        }
        if (args.Length > 0 && string.Equals(args[0], "--voice-test", StringComparison.OrdinalIgnoreCase))
        {
            string text = args.Length > 1
                ? string.Join(" ", args.Skip(1))
                : "Arthur voice test complete.";
            RunVoiceTest(text);
            return;
        }
        if (args.Length > 0 && string.Equals(args[0], "--body-calibrate", StringComparison.OrdinalIgnoreCase))
        {
            bool strict = !args.Skip(1).Any(a => string.Equals(a, "--non-strict", StringComparison.OrdinalIgnoreCase));
            RunBodyCalibration(strict);
            return;
        }
        if (args.Length > 0 && string.Equals(args[0], "--seated-handshake-test", StringComparison.OrdinalIgnoreCase))
        {
            int shakes = ParseTopCount(args, 1, 3);
            RunSeatedHandshakeTest(shakes);
            return;
        }
        if (args.Length > 0 && string.Equals(args[0], "--safety-report", StringComparison.OrdinalIgnoreCase))
        {
            int topN = ParseTopCount(args, 1, 5);
            RunSafetyReport(pathOverride: null, topN);
            return;
        }
        if (args.Length > 1 && string.Equals(args[0], "--safety-report-file", StringComparison.OrdinalIgnoreCase))
        {
            int topN = ParseTopCount(args, 2, 5);
            RunSafetyReport(args[1], topN);
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
        int thermalViolations = 0;
        int voltageViolations = 0;
        int commErrors = 0;
        for (int i = 0; i < snapshot.Count; i++)
        {
            var row = snapshot[i];
            if (row.Overload) overloads++;
            if (row.ThermalViolation) thermalViolations++;
            if (row.VoltageViolation) voltageViolations++;
            if (!row.CommunicationOk) commErrors++;
            Console.WriteLine(
                $"{row.MotorName}({row.ID}) zone={row.Location} torque={(row.TorqueOn ? "ON" : "OFF")} " +
                $"load={row.Load}/{row.OverloadThreshold} temp={row.Temperature}/{row.MaxTemperature} " +
                $"volt={row.Voltage}/{row.MinVoltage} overload={row.Overload} thermal={row.ThermalViolation} " +
                $"voltage={row.VoltageViolation} comm={(row.CommunicationOk ? "OK" : "ERR")}");
        }
        Console.WriteLine(
            $"SUMMARY total={snapshot.Count} overloads={overloads} thermal={thermalViolations} " +
            $"voltage={voltageViolations} comm_errors={commErrors}");
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

    static void RunImuProbe()
    {
        RobotControlService service = new();
        for (int i = 0; i < 8; i++)
        {
            Console.WriteLine(service.ReadImuTelemetry());
            Thread.Sleep(250);
        }
    }

    static void RunSeatedHandshakeTest(int shakes)
    {
        RobotControlService service = new();
        Console.WriteLine(service.Initialize());
        Console.WriteLine(service.ExecuteSeatedHandshakeSafetyTest(shakes: shakes, stepDurationMs: 450, interpolationSteps: 8));
    }

    static void RunBodyCalibration(bool strict)
    {
        RobotControlService service = new();
        Console.WriteLine(service.Initialize());
        Console.WriteLine(service.RunStartupBodyAwarenessCalibration(strict));
    }

    static void RunVoiceTest(string text)
    {
        using RobotNarrationService voice = new();
        Console.WriteLine($"VOICE status={voice.Status}");
        if (!voice.IsAvailable)
            return;

        voice.Announce(text);
        Console.WriteLine("VOICE spoke test phrase.");
    }

    static int ParseTopCount(string[] args, int index, int fallback)
    {
        if (args.Length <= index)
            return fallback;
        if (!int.TryParse(args[index], out int parsed) || parsed <= 0)
            return fallback;
        return parsed;
    }

    static void RunSafetyReport(string pathOverride, int topN)
    {
        string[] candidates = ResolveSafetyReportCandidates(pathOverride);
        string logPath = candidates.FirstOrDefault(File.Exists) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(logPath))
        {
            Console.WriteLine("SAFETY_REPORT status=no-log-file");
            Console.WriteLine("Checked paths:");
            foreach (string candidate in candidates)
                Console.WriteLine($"  - {candidate}");
            return;
        }

        Dictionary<string, int> eventCounts = new(StringComparer.Ordinal);
        Dictionary<string, int> actionCounts = new(StringComparer.Ordinal);
        Dictionary<string, int> phaseCounts = new(StringComparer.Ordinal);
        Dictionary<string, int> motorCounts = new(StringComparer.Ordinal);
        Dictionary<string, int> guardrailCounts = new(StringComparer.Ordinal);
        Queue<string> recentTrips = new();

        int totalLines = 0;
        int malformed = 0;
        int safetyTrips = 0;

        foreach (string rawLine in File.ReadLines(logPath))
        {
            string line = rawLine?.Trim() ?? string.Empty;
            if (line.Length == 0)
                continue;

            totalLines++;
            if (!TryParseSafetyLogLine(line, out string timestamp, out Dictionary<string, string> fields))
            {
                malformed++;
                continue;
            }

            string eventType = GetField(fields, "event");
            if (eventType.Length > 0)
                Increment(eventCounts, eventType);

            if (!string.Equals(eventType, "SAFETY_GATE_TRIP", StringComparison.Ordinal))
                continue;

            safetyTrips++;
            string action = GetField(fields, "action");
            string phase = GetField(fields, "phase");
            string scope = GetField(fields, "scope");
            string detail = GetField(fields, "detail");

            if (action.Length > 0)
                Increment(actionCounts, action);
            if (phase.Length > 0)
                Increment(phaseCounts, phase);

            foreach (string motor in ExtractMotors(scope, detail))
                Increment(motorCounts, motor);
            foreach ((string guardrail, bool active) in ExtractGuardrails(detail))
            {
                if (active)
                    Increment(guardrailCounts, guardrail);
            }

            string summary = $"[{timestamp}] action={action} phase={phase} scope={scope}";
            if (recentTrips.Count == 5)
                recentTrips.Dequeue();
            recentTrips.Enqueue(summary);
        }

        Console.WriteLine($"SAFETY_REPORT file={logPath}");
        Console.WriteLine($"LINES total={totalLines} safety_trips={safetyTrips} malformed={malformed}");
        PrintTop("EVENTS", eventCounts, topN);
        PrintTop("ACTIONS", actionCounts, topN);
        PrintTop("PHASES", phaseCounts, topN);
        PrintTop("MOTORS", motorCounts, topN);
        PrintTop("GUARDRAILS", guardrailCounts, topN);

        Console.WriteLine("RECENT_TRIPS");
        if (recentTrips.Count == 0)
        {
            Console.WriteLine("  none");
        }
        else
        {
            foreach (string summary in recentTrips)
                Console.WriteLine($"  {summary}");
        }
    }

    static string[] ResolveSafetyReportCandidates(string pathOverride)
    {
        if (!string.IsNullOrWhiteSpace(pathOverride))
            return new[] { Path.GetFullPath(pathOverride) };

        string runtimePath = Path.Combine(AppContext.BaseDirectory, "logs", "safety-events.log");
        string workspacePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "logs", "safety-events.log"));

        return new[] { runtimePath, workspacePath };
    }

    static bool TryParseSafetyLogLine(string line, out string timestamp, out Dictionary<string, string> fields)
    {
        timestamp = string.Empty;
        fields = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] parts = line.Split('\t');
        if (parts.Length < 2)
            return false;

        timestamp = parts[0].Trim();
        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i];
            int equalsIndex = part.IndexOf('=');
            if (equalsIndex <= 0 || equalsIndex == part.Length - 1)
                continue;

            string key = part.Substring(0, equalsIndex).Trim();
            string value = part.Substring(equalsIndex + 1).Trim();
            if (key.Length > 0)
                fields[key] = value;
        }

        return fields.Count > 0;
    }

    static string GetField(IReadOnlyDictionary<string, string> fields, string key)
    {
        if (fields.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
            return value;
        return string.Empty;
    }

    static IEnumerable<string> ExtractMotors(string scope, string detail)
    {
        HashSet<string> results = new(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(scope) &&
            !string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string motor in scope.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                results.Add(motor);
        }

        if (!string.IsNullOrWhiteSpace(detail))
        {
            foreach ((string _, string value) in ParseDetailSections(detail))
            {
                foreach (string token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    string normalizedToken = NormalizeToken(token);
                    if (string.Equals(normalizedToken, "none", StringComparison.OrdinalIgnoreCase))
                        continue;
                    int colonIndex = normalizedToken.IndexOf(':');
                    string motor = colonIndex > 0
                        ? NormalizeToken(normalizedToken.Substring(0, colonIndex))
                        : normalizedToken;
                    if (motor.Length > 0 && !string.Equals(motor, "none", StringComparison.OrdinalIgnoreCase))
                        results.Add(motor);
                }
            }
        }

        return results;
    }

    static IEnumerable<(string Guardrail, bool Active)> ExtractGuardrails(string detail)
    {
        Dictionary<string, string> sections = ParseDetailSections(detail);
        return new (string Guardrail, bool Active)[]
        {
            ("Comm", IsActiveSection(sections, "Comm")),
            ("Overload", IsActiveSection(sections, "Overload")),
            ("Thermal", IsActiveSection(sections, "Thermal")),
            ("Voltage", IsActiveSection(sections, "Voltage")),
            ("TorqueOff", IsActiveSection(sections, "TorqueOff"))
        };
    }

    static bool IsActiveSection(IReadOnlyDictionary<string, string> sections, string key)
    {
        if (!sections.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
            return false;
        return !string.Equals(NormalizeToken(value), "none", StringComparison.OrdinalIgnoreCase);
    }

    static Dictionary<string, string> ParseDetailSections(string detail)
    {
        Dictionary<string, string> sections = new(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(detail))
            return sections;

        foreach (string segment in detail.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int equalsIndex = segment.IndexOf('=');
            if (equalsIndex <= 0 || equalsIndex == segment.Length - 1)
                continue;

            string key = segment.Substring(0, equalsIndex).Trim();
            string value = segment.Substring(equalsIndex + 1).Trim();
            if (key.Length > 0)
                sections[key] = value;
        }

        return sections;
    }

    static string NormalizeToken(string value)
    {
        return value.Trim().TrimEnd('.');
    }

    static void PrintTop(string title, IReadOnlyDictionary<string, int> counts, int topN)
    {
        Console.WriteLine(title);
        if (counts.Count == 0)
        {
            Console.WriteLine("  none");
            return;
        }

        foreach ((string key, int value) in counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(topN))
        {
            Console.WriteLine($"  {key}: {value}");
        }
    }

    static void Increment(IDictionary<string, int> counts, string key)
    {
        if (counts.TryGetValue(key, out int existing))
            counts[key] = existing + 1;
        else
            counts[key] = 1;
    }
}
