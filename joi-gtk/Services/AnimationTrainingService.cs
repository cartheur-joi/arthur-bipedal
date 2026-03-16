using Cartheur.Animals.Robot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace joi_gtk.Services;

public sealed class AnimationTrainingService
{
    readonly RobotControlService _robot;
    readonly Remember _trainingStore;
    readonly List<Dictionary<string, int>> _capturedFrames = new();
    string[] _activeMotors = Limbic.LeftArm;
    string _activeTrainingType = string.Empty;

    public AnimationTrainingService(RobotControlService robot)
    {
        _robot = robot;
        _trainingStore = new Remember(@"\db\trainings.db");
    }

    public int CapturedFrameCount => _capturedFrames.Count;

    public Dictionary<string, int> BeginSession(string armSelection, string replayPhrase)
    {
        _robot.EnforceStableSittingPosition(durationMilliseconds: 900, interpolationSteps: 8, positionTolerance: 15);
        string normalizedPhrase = NormalizeReplayPhrase(replayPhrase);
        _activeMotors = ResolveArmMotors(armSelection);
        _activeTrainingType = BuildTrainingType(normalizedPhrase, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _capturedFrames.Clear();
        _robot.SetTorqueOff(_activeMotors);
        return CaptureStep();
    }

    public Dictionary<string, int> CaptureStep()
    {
        Dictionary<string, int> frame = _robot.ReadPositions(_activeMotors);
        _capturedFrames.Add(new Dictionary<string, int>(frame));
        return frame;
    }

    public int StopAndSaveSession()
    {
        if (string.IsNullOrWhiteSpace(_activeTrainingType))
            throw new InvalidOperationException("No active animation training session.");
        if (_capturedFrames.Count == 0)
            throw new InvalidOperationException("No captured frames to save.");

        int sequence = 1;
        foreach (Dictionary<string, int> frame in _capturedFrames)
        {
            _trainingStore.StoreTrainingSequence(sequence, _activeTrainingType, frame, _trainingStore.DataBaseTag);
            sequence++;
        }

        int frameCount = _capturedFrames.Count;
        _capturedFrames.Clear();
        _activeTrainingType = string.Empty;
        _robot.SetTorqueOn(_activeMotors);
        return frameCount;
    }

    public int ReplayLatest(string replayPhrase, int stepDurationMs = 700)
    {
        _robot.EnforceStableSittingPosition(durationMilliseconds: 900, interpolationSteps: 8, positionTolerance: 15);
        string normalizedPhrase = NormalizeReplayPhrase(replayPhrase);
        List<Dictionary<string, int>> frames = LoadLatestFrames(normalizedPhrase);
        if (frames.Count == 0)
            throw new InvalidOperationException($"No training session found for replay phrase '{normalizedPhrase}'.");

        foreach (Dictionary<string, int> frame in frames)
        {
            _robot.MoveToPositions(frame, stepDurationMs, 6);
        }

        return frames.Count;
    }

    static string[] ResolveArmMotors(string armSelection)
    {
        if (string.Equals(armSelection, "Right Arm", StringComparison.OrdinalIgnoreCase))
            return Limbic.RightArm;
        return Limbic.LeftArm;
    }

    static string NormalizeReplayPhrase(string replayPhrase)
    {
        if (string.IsNullOrWhiteSpace(replayPhrase))
            throw new InvalidOperationException("Replay phrase is required.");
        return replayPhrase.Trim().ToLowerInvariant();
    }

    static string BuildTrainingType(string normalizedPhrase, long sessionId)
    {
        return $"{normalizedPhrase}::{sessionId}";
    }

    List<Dictionary<string, int>> LoadLatestFrames(string normalizedPhrase)
    {
        DataSet ds = _trainingStore.RetrieveData("TrainingSequence");
        if (ds == null || ds.Tables.Count == 0)
            return new List<Dictionary<string, int>>();

        DataTable table = ds.Tables[0];
        string prefix = normalizedPhrase + "::";

        List<(long SessionId, int Sequence, string Motor, int Position)> rows = new();
        foreach (DataRow row in table.Rows)
        {
            string trainingType = Convert.ToString(row["TrainingType"]) ?? string.Empty;
            if (!trainingType.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            if (!long.TryParse(trainingType.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out long sessionId))
                continue;

            int sequence = Convert.ToInt32(row["SequenceNumber"], CultureInfo.InvariantCulture);
            string motor = Convert.ToString(row["Motor"]) ?? string.Empty;
            int position = Convert.ToInt32(row["Position"], CultureInfo.InvariantCulture);
            rows.Add((sessionId, sequence, motor, position));
        }

        if (rows.Count == 0)
            return new List<Dictionary<string, int>>();

        long latestSession = rows.Max(r => r.SessionId);
        return rows
            .Where(r => r.SessionId == latestSession)
            .GroupBy(r => r.Sequence)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                Dictionary<string, int> frame = new();
                foreach (var item in g)
                    frame[item.Motor] = item.Position;
                return frame;
            })
            .ToList();
    }
}
