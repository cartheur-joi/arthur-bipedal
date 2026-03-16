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
    readonly object _sessionGate = new();
    readonly List<Dictionary<string, int>> _capturedFrames = new();
    string[] _activeMotors = Limbic.LeftArm;
    string _activeTrainingType = string.Empty;

    public AnimationTrainingService(RobotControlService robot)
    {
        _robot = robot;
        _trainingStore = new Remember(@"\db\trainings.db");
    }

    public int CapturedFrameCount
    {
        get
        {
            lock (_sessionGate)
                return _capturedFrames.Count;
        }
    }

    public Dictionary<string, int> BeginSession(string armSelection, string replayPhrase)
    {
        _robot.EnforceStableSittingPosition(durationMilliseconds: 900, interpolationSteps: 8, positionTolerance: 15);
        string normalizedPhrase = NormalizeReplayPhrase(replayPhrase);
        string[] activeMotors = ResolveArmMotors(armSelection);
        lock (_sessionGate)
        {
            _activeMotors = activeMotors;
            _activeTrainingType = BuildTrainingType(normalizedPhrase, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _capturedFrames.Clear();
        }
        _robot.SetTorqueOff(activeMotors);
        return CaptureStep();
    }

    public Dictionary<string, int> CaptureStep()
    {
        string[] activeMotors;
        lock (_sessionGate)
        {
            if (string.IsNullOrWhiteSpace(_activeTrainingType))
                throw new InvalidOperationException("No active animation training session.");
            activeMotors = _activeMotors.ToArray();
        }

        Dictionary<string, int> frame = _robot.ReadPositions(activeMotors);
        lock (_sessionGate)
            _capturedFrames.Add(new Dictionary<string, int>(frame));
        return frame;
    }

    public int StopAndSaveSession()
    {
        string trainingType;
        string[] activeMotors;
        List<Dictionary<string, int>> framesToPersist;
        lock (_sessionGate)
        {
            if (string.IsNullOrWhiteSpace(_activeTrainingType))
                throw new InvalidOperationException("No active animation training session.");
            if (_capturedFrames.Count == 0)
                throw new InvalidOperationException("No captured frames to save.");

            trainingType = _activeTrainingType;
            activeMotors = _activeMotors.ToArray();
            framesToPersist = _capturedFrames
                .Select(frame => new Dictionary<string, int>(frame))
                .ToList();
            _capturedFrames.Clear();
            _activeTrainingType = string.Empty;
        }

        int sequence = 1;
        foreach (Dictionary<string, int> frame in framesToPersist)
        {
            _trainingStore.StoreTrainingSequence(sequence, trainingType, frame, _trainingStore.DataBaseTag);
            sequence++;
        }

        int frameCount = framesToPersist.Count;
        _robot.SetTorqueOn(activeMotors);
        return frameCount;
    }

    public int ReplayLatest(string replayPhrase, int stepDurationMs = 700)
    {
        _robot.EnforceStableSittingPosition(durationMilliseconds: 900, interpolationSteps: 8, positionTolerance: 15);
        string normalizedPhrase = NormalizeReplayPhrase(replayPhrase);
        List<Dictionary<string, int>> frames = LoadLatestFrames(normalizedPhrase);
        if (frames.Count == 0)
            throw new InvalidOperationException($"No training session found for replay phrase '{normalizedPhrase}'.");

        Exception replayFailure = null;
        foreach (Dictionary<string, int> frame in frames)
        {
            try
            {
                _robot.MoveToPositions(frame, stepDurationMs, 6);
            }
            catch (Exception ex)
            {
                replayFailure = ex;
                break;
            }
        }

        Exception recoveryFailure = null;
        try
        {
            _robot.EnforceStableSittingPosition(durationMilliseconds: 900, interpolationSteps: 8, positionTolerance: 15);
        }
        catch (Exception ex)
        {
            recoveryFailure = ex;
        }

        if (replayFailure != null && recoveryFailure != null)
        {
            throw new InvalidOperationException(
                $"Replay failed ({replayFailure.Message}) and seated recovery failed ({recoveryFailure.Message}).",
                replayFailure);
        }
        if (replayFailure != null)
            throw replayFailure;
        if (recoveryFailure != null)
            throw new InvalidOperationException($"Replay completed but seated recovery failed: {recoveryFailure.Message}", recoveryFailure);

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
