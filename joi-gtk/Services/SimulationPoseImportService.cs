using Cartheur.Animals.Robot;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace joi_gtk.Services;

public sealed class SimulationPoseImportService
{
    readonly string _workspaceDirectory;
    readonly string _runtimeDirectory;
    readonly string _bodyModelPath;

    public SimulationPoseImportService(string workspaceDirectory, string runtimeDirectory)
    {
        _workspaceDirectory = string.IsNullOrWhiteSpace(workspaceDirectory)
            ? AppContext.BaseDirectory
            : Path.GetFullPath(workspaceDirectory);
        _runtimeDirectory = string.IsNullOrWhiteSpace(runtimeDirectory)
            ? AppContext.BaseDirectory
            : Path.GetFullPath(runtimeDirectory);
        _bodyModelPath = Path.Combine(_workspaceDirectory, "joi-gtk", "config", "body-model.json");
        if (!File.Exists(_bodyModelPath))
            _bodyModelPath = Path.Combine(_runtimeDirectory, "config", "body-model.json");
    }

    public SimulationImportResult ImportTrainingSequence(string jsonPath, string replayPhrase)
    {
        string resolvedJsonPath = ResolvePath(jsonPath);
        SimulationSession session = LoadSession(resolvedJsonPath);
        string normalizedPhrase = NormalizeReplayPhrase(replayPhrase);
        string trainingType = normalizedPhrase + "::" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string databasePath = Path.Combine(_runtimeDirectory, "db", "trainings.db");

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? _runtimeDirectory);
        using SqliteConnection connection = new("Data Source=" + databasePath);
        connection.Open();
        EnsureTrainingSchema(connection);

        using SqliteTransaction transaction = connection.BeginTransaction();
        for (int i = 0; i < session.Frames.Count; i++)
        {
            int sequence = i + 1;
            foreach ((string motor, int position) in session.Frames[i].JointTargets.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    "INSERT INTO TrainingSequence (SequenceNumber, TrainingType, Motor, Position) " +
                    "VALUES (@sequenceNumber, @trainingType, @motor, @position)";
                command.Parameters.AddWithValue("@sequenceNumber", sequence);
                command.Parameters.AddWithValue("@trainingType", trainingType);
                command.Parameters.AddWithValue("@motor", motor);
                command.Parameters.AddWithValue("@position", position);
                command.ExecuteNonQuery();
            }
        }

        transaction.Commit();
        return BuildResult(session, resolvedJsonPath, trainingType, outputPath: null);
    }

    public SimulationImportResult ExportJointVectorLog(string jsonPath, string outputPath)
    {
        string resolvedJsonPath = ResolvePath(jsonPath);
        SimulationSession session = LoadSession(resolvedJsonPath);
        string resolvedOutputPath = ResolvePath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath) ?? _workspaceDirectory);

        using JointVectorFrameRecorder recorder = new(resolvedOutputPath, append: false);
        long sequenceNumber = 1;
        foreach (SimulationFrame frame in session.Frames)
        {
            JointVectorFrame message = new(
                frame.JointTargets,
                frame.DurationMilliseconds ?? session.DefaultDurationMs,
                frame.InterpolationSteps ?? session.DefaultInterpolationSteps,
                sequenceNumber++);
            recorder.Record(message);
        }

        return BuildResult(session, resolvedJsonPath, trainingType: null, resolvedOutputPath);
    }

    SimulationImportResult BuildResult(
        SimulationSession session,
        string jsonPath,
        string trainingType,
        string outputPath)
    {
        HashSet<string> knownJoints = LoadKnownJoints();
        HashSet<string> allTargets = session.Frames
            .SelectMany(frame => frame.JointTargets.Keys)
            .ToHashSet(StringComparer.Ordinal);
        List<string> unknownJoints = allTargets
            .Where(joint => knownJoints.Count > 0 && !knownJoints.Contains(joint))
            .OrderBy(joint => joint, StringComparer.Ordinal)
            .ToList();

        return new SimulationImportResult(
            Path.GetFullPath(jsonPath),
            session.SessionName,
            session.Frames.Count,
            allTargets.Count,
            trainingType,
            outputPath,
            unknownJoints);
    }

    SimulationSession LoadSession(string resolvedPath)
    {
        if (!File.Exists(resolvedPath))
            throw new FileNotFoundException("Simulation session JSON not found.", resolvedPath);

        SimulationSessionFile file = JsonSerializer.Deserialize<SimulationSessionFile>(
            File.ReadAllText(resolvedPath),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? throw new InvalidOperationException("Failed to parse simulation session JSON.");

        string sessionName = string.IsNullOrWhiteSpace(file.SessionName)
            ? Path.GetFileNameWithoutExtension(resolvedPath)
            : file.SessionName.Trim();
        int defaultDurationMs = file.DefaultDurationMs.GetValueOrDefault(700);
        int defaultInterpolationSteps = file.DefaultInterpolationSteps.GetValueOrDefault(6);
        if (defaultDurationMs <= 0)
            throw new InvalidOperationException("defaultDurationMs must be greater than zero.");
        if (defaultInterpolationSteps <= 0)
            throw new InvalidOperationException("defaultInterpolationSteps must be greater than zero.");
        if (file.Frames == null || file.Frames.Count == 0)
            throw new InvalidOperationException("Simulation session must contain at least one frame.");

        List<SimulationFrame> frames = new();
        for (int i = 0; i < file.Frames.Count; i++)
        {
            SimulationFrameFile frame = file.Frames[i] ?? throw new InvalidOperationException($"Frame {i + 1} is null.");
            if (frame.JointTargets == null || frame.JointTargets.Count == 0)
                throw new InvalidOperationException($"Frame {i + 1} must contain at least one joint target.");

            Dictionary<string, int> normalizedTargets = new(StringComparer.Ordinal);
            foreach ((string rawJoint, int position) in frame.JointTargets)
            {
                string joint = (rawJoint ?? string.Empty).Trim();
                if (joint.Length == 0)
                    throw new InvalidOperationException($"Frame {i + 1} contains an empty joint name.");
                normalizedTargets[joint] = position;
            }

            frames.Add(new SimulationFrame(
                frame.Name?.Trim() ?? $"frame_{i + 1:D3}",
                frame.DurationMs,
                frame.InterpolationSteps,
                normalizedTargets));
        }

        return new SimulationSession(sessionName, defaultDurationMs, defaultInterpolationSteps, frames);
    }

    HashSet<string> LoadKnownJoints()
    {
        if (!File.Exists(_bodyModelPath))
            return new HashSet<string>(StringComparer.Ordinal);

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(_bodyModelPath));
        if (!document.RootElement.TryGetProperty("joints", out JsonElement jointsElement) ||
            jointsElement.ValueKind != JsonValueKind.Object)
            return new HashSet<string>(StringComparer.Ordinal);

        HashSet<string> joints = new(StringComparer.Ordinal);
        foreach (JsonProperty property in jointsElement.EnumerateObject())
            joints.Add(property.Name);
        return joints;
    }

    string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Path is required.");
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(_workspaceDirectory, path));
    }

    static string NormalizeReplayPhrase(string replayPhrase)
    {
        if (string.IsNullOrWhiteSpace(replayPhrase))
            throw new InvalidOperationException("Replay phrase is required.");
        return replayPhrase.Trim().ToLowerInvariant();
    }

    static void EnsureTrainingSchema(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE IF NOT EXISTS TrainingSequence (" +
            "SequenceNumber INTEGER NOT NULL, " +
            "TrainingType TEXT NOT NULL, " +
            "Motor TEXT NOT NULL, " +
            "Position INTEGER NOT NULL);" +
            "CREATE INDEX IF NOT EXISTS idx_training_sequence_type_seq " +
            "ON TrainingSequence(TrainingType, SequenceNumber);";
        command.ExecuteNonQuery();
    }

    sealed record SimulationSession(string SessionName, int DefaultDurationMs, int DefaultInterpolationSteps, List<SimulationFrame> Frames);
    sealed record SimulationFrame(string Name, int? DurationMilliseconds, int? InterpolationSteps, Dictionary<string, int> JointTargets);

    sealed class SimulationSessionFile
    {
        public string SessionName { get; set; }
        public int? DefaultDurationMs { get; set; }
        public int? DefaultInterpolationSteps { get; set; }
        public List<SimulationFrameFile> Frames { get; set; }
    }

    sealed class SimulationFrameFile
    {
        public string Name { get; set; }
        public int? DurationMs { get; set; }
        public int? InterpolationSteps { get; set; }
        public Dictionary<string, int> JointTargets { get; set; }
    }
}

public sealed record SimulationImportResult(
    string SourcePath,
    string SessionName,
    int FrameCount,
    int UniqueJointCount,
    string TrainingType,
    string OutputPath,
    IReadOnlyList<string> UnknownJoints);
