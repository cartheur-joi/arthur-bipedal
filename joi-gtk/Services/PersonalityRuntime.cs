using System;
using System.Collections.Generic;

namespace joi_gtk.Services;

public interface IPersonalityRuntime
{
    string Name { get; }
    bool IsEnabled { get; }
    string Status { get; }
    string AdaptSpeech(string baseText, string userInputHint = "");
}

public static class PersonalityRuntimeFactory
{
    public static IPersonalityRuntime Create()
    {
        if (!IsPersonalityEnabled())
            return new NullPersonalityRuntime("Personality disabled (ARTHUR_PERSONALITY_ENABLED=0).");

        string personality = (Environment.GetEnvironmentVariable("ARTHUR_PERSONALITY") ?? "arthur").Trim();
        if (string.Equals(personality, "lubos", StringComparison.OrdinalIgnoreCase))
            return new LubosPersonalityRuntime();

        return new NullPersonalityRuntime($"Personality bypassed (ARTHUR_PERSONALITY={personality}).");
    }

    static bool IsPersonalityEnabled()
    {
        string raw = Environment.GetEnvironmentVariable("ARTHUR_PERSONALITY_ENABLED") ?? string.Empty;
        if (string.Equals(raw, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "off", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }
}

sealed class NullPersonalityRuntime : IPersonalityRuntime
{
    public NullPersonalityRuntime(string status)
    {
        Status = status;
    }

    public string Name => "None";
    public bool IsEnabled => false;
    public string Status { get; }

    public string AdaptSpeech(string baseText, string userInputHint = "")
    {
        return baseText ?? string.Empty;
    }
}

sealed class LubosPersonalityRuntime : IPersonalityRuntime
{
    readonly object _gate = new();
    readonly EmotionalLearningRuntime _learning;

    public LubosPersonalityRuntime()
    {
        _learning = new EmotionalLearningRuntime(new EmotionalLearningProfile { Personality = "Lubos" });
    }

    public string Name => "Lubos";
    public bool IsEnabled => true;
    public string Status => "Personality active (Lubos, adaptive emotional runtime).";

    public string AdaptSpeech(string baseText, string userInputHint = "")
    {
        string output = baseText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(output))
            return string.Empty;

        lock (_gate)
        {
            string hint = string.IsNullOrWhiteSpace(userInputHint) ? output : userInputHint;
            EmotionalObservation observation = EmotionalStateTracker.DetectFromText(hint);
            EmotionalTurnDecision decision = _learning.StartTurn(observation);
            string adapted = _learning.AdaptOutput(output, decision.Action, "Lubos");
            adapted = ApplyLubosTone(adapted);
            _learning.RecordTurn(observation, decision.Action, adapted);
            return adapted;
        }
    }

    static string ApplyLubosTone(string text)
    {
        string normalized = text.Trim();
        if (normalized.Length == 0)
            return normalized;

        if (normalized.Contains("failed", StringComparison.OrdinalIgnoreCase))
            return normalized + " We will recover safely.";

        if (normalized.Contains("completed", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(": OK", StringComparison.OrdinalIgnoreCase))
            return normalized + " Nice work.";

        if (normalized.StartsWith("I will ", StringComparison.OrdinalIgnoreCase))
            return "Let us " + normalized.Substring("I will ".Length);

        return normalized;
    }
}

sealed class EmotionalTurnDecision
{
    public string Action { get; set; } = "neutral";
    public bool RewardApplied { get; set; }
    public double Reward { get; set; }
}

sealed class EmotionalLearningRuntime
{
    static readonly string[] ActionSet = { "neutral", "comfort", "playful", "coach" };
    readonly Random _random = new();
    readonly EmotionalLearningProfile _profile;
    string _lastObservedEmotion = "Neutral";
    string _pendingAction = string.Empty;

    public EmotionalLearningRuntime(EmotionalLearningProfile profile)
    {
        _profile = profile ?? new EmotionalLearningProfile();
        EnsureActionValues();
    }

    public EmotionalTurnDecision StartTurn(EmotionalObservation observation)
    {
        EmotionalTurnDecision decision = new() { Action = "neutral" };
        if (observation == null)
            return decision;

        if (!string.IsNullOrWhiteSpace(_pendingAction))
        {
            double reward = ComputeReward(_lastObservedEmotion, observation.Emotion, observation.Confidence, observation.EngagementScore);
            double alpha = Math.Clamp(_profile.Alpha, 0.05, 0.95);
            double previous = _profile.ActionValues.TryGetValue(_pendingAction, out double value) ? value : 0.0;
            _profile.ActionValues[_pendingAction] = previous + alpha * (reward - previous);
            _profile.LastReward = reward;
            _profile.LastAction = _pendingAction;
            _profile.Episodes++;
            decision.RewardApplied = true;
            decision.Reward = reward;
        }

        string nextAction = SelectAction(observation);
        _pendingAction = nextAction;
        _lastObservedEmotion = observation.Emotion;
        decision.Action = nextAction;
        return decision;
    }

    public string AdaptOutput(string output, string action, string personality)
    {
        string baseOutput = output ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseOutput))
            return baseOutput;

        if (string.Equals(action, "comfort", StringComparison.OrdinalIgnoreCase))
            return "I'm here with you. " + baseOutput;

        if (string.Equals(action, "playful", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(personality, "Samantha", StringComparison.OrdinalIgnoreCase))
                return baseOutput + " Let's keep it bright together.";
            return baseOutput + " We can keep this light and fun.";
        }

        if (string.Equals(action, "coach", StringComparison.OrdinalIgnoreCase))
            return baseOutput + " Let's take it one step at a time.";

        return baseOutput;
    }

    public void RecordTurn(EmotionalObservation observation, string action, string output)
    {
        if (observation == null)
            return;

        _profile.RecentTurns.Add(new EmotionalTurnRecord
        {
            TimestampUtc = observation.TimestampUtc,
            UserEmotion = observation.Emotion,
            Confidence = observation.Confidence,
            EngagementScore = observation.EngagementScore,
            Action = string.IsNullOrWhiteSpace(action) ? "neutral" : action,
            Reward = _profile.LastReward,
            UserInputLength = (observation.UserInput ?? string.Empty).Length,
            OutputLength = (output ?? string.Empty).Length
        });

        const int maxTurns = 300;
        if (_profile.RecentTurns.Count > maxTurns)
            _profile.RecentTurns.RemoveRange(0, _profile.RecentTurns.Count - maxTurns);
    }

    string SelectAction(EmotionalObservation observation)
    {
        double confidence = Math.Clamp(observation.Confidence, 0.0, 1.0);
        if (confidence < _profile.MinConfidence)
            return "neutral";

        string emotion = observation.Emotion ?? "Neutral";
        int valence = EmotionalStateTracker.ToValence(emotion);
        if (valence < 0 && _random.NextDouble() < 0.70)
            return "comfort";
        if (valence > 0 && _random.NextDouble() < 0.55)
            return "playful";

        double epsilon = Math.Clamp(_profile.Epsilon, 0.01, 0.60);
        if (_random.NextDouble() < epsilon)
            return ActionSet[_random.Next(ActionSet.Length)];

        string bestAction = "neutral";
        double bestValue = double.MinValue;
        foreach (KeyValuePair<string, double> pair in _profile.ActionValues)
        {
            if (pair.Value > bestValue)
            {
                bestValue = pair.Value;
                bestAction = pair.Key;
            }
        }

        return bestAction;
    }

    static double ComputeReward(string previousEmotion, string currentEmotion, double confidence, double engagementScore)
    {
        int previousValence = EmotionalStateTracker.ToValence(previousEmotion);
        int currentValence = EmotionalStateTracker.ToValence(currentEmotion);
        double moodShift = currentValence - previousValence;
        double confidenceWeight = Math.Clamp(confidence, 0.10, 1.00);
        double engagement = Math.Clamp(engagementScore, 0.0, 1.0);
        double reward = (0.75 * moodShift) + (0.25 * engagement);
        reward *= confidenceWeight;
        return Math.Clamp(reward, -1.0, 1.0);
    }

    void EnsureActionValues()
    {
        foreach (string action in ActionSet)
        {
            if (!_profile.ActionValues.ContainsKey(action))
                _profile.ActionValues[action] = 0.0;
        }
    }
}

static class EmotionalStateTracker
{
    public static EmotionalObservation DetectFromText(string userInput)
    {
        string normalized = (userInput ?? string.Empty).Trim().ToLowerInvariant();
        EmotionalObservation observation = new()
        {
            TimestampUtc = DateTime.UtcNow,
            UserInput = userInput ?? string.Empty,
            Emotion = "Neutral",
            Confidence = 0.50,
            EngagementScore = ComputeEngagementScore(normalized)
        };

        if (string.IsNullOrWhiteSpace(normalized))
        {
            observation.Confidence = 0.25;
            return observation;
        }

        int sadHits = CountHits(normalized, "sad", "upset", "angry", "frustrated", "tired", "depressed", "anxious", "stressed", "lonely", "bad");
        int happyHits = CountHits(normalized, "happy", "great", "good", "joy", "excited", "awesome", "better", "thanks", "thank you", "love");

        if (sadHits > happyHits)
        {
            observation.Emotion = "Sad";
            observation.Confidence = Math.Clamp(0.55 + (sadHits - happyHits) * 0.08, 0.55, 0.92);
        }
        else if (happyHits > sadHits)
        {
            observation.Emotion = "Happy";
            observation.Confidence = Math.Clamp(0.55 + (happyHits - sadHits) * 0.08, 0.55, 0.92);
        }

        return observation;
    }

    public static int ToValence(string emotion)
    {
        if (string.Equals(emotion, "Happy", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (string.Equals(emotion, "Sad", StringComparison.OrdinalIgnoreCase))
            return -1;
        return 0;
    }

    static int CountHits(string text, params string[] tokens)
    {
        int hits = 0;
        foreach (string token in tokens)
        {
            if (text.Contains(token, StringComparison.Ordinal))
                hits++;
        }
        return hits;
    }

    static double ComputeEngagementScore(string normalizedInput)
    {
        if (string.IsNullOrWhiteSpace(normalizedInput))
            return 0.0;

        double lengthFactor = Math.Clamp(normalizedInput.Length / 120.0, 0.05, 1.0);
        double questionFactor = normalizedInput.Contains("?") ? 0.20 : 0.0;
        int words = normalizedInput.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        double wordsFactor = Math.Clamp(words / 16.0, 0.05, 1.0);
        double score = (lengthFactor * 0.45) + (wordsFactor * 0.45) + questionFactor;
        return Math.Clamp(score, 0.0, 1.0);
    }
}

sealed class EmotionalObservation
{
    public DateTime TimestampUtc { get; set; }
    public string Emotion { get; set; } = "Neutral";
    public double Confidence { get; set; }
    public double EngagementScore { get; set; }
    public string UserInput { get; set; } = string.Empty;
}

sealed class EmotionalTurnRecord
{
    public DateTime TimestampUtc { get; set; }
    public string UserEmotion { get; set; } = "Neutral";
    public double Confidence { get; set; }
    public double EngagementScore { get; set; }
    public string Action { get; set; } = "neutral";
    public double Reward { get; set; }
    public int UserInputLength { get; set; }
    public int OutputLength { get; set; }
}

sealed class EmotionalLearningProfile
{
    public string Personality { get; set; } = string.Empty;
    public int Episodes { get; set; }
    public double Epsilon { get; set; } = 0.18;
    public double Alpha { get; set; } = 0.35;
    public double MinConfidence { get; set; } = 0.45;
    public string LastAction { get; set; } = "neutral";
    public double LastReward { get; set; }
    public Dictionary<string, double> ActionValues { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["neutral"] = 0.0,
        ["comfort"] = 0.0,
        ["playful"] = 0.0,
        ["coach"] = 0.0
    };
    public List<EmotionalTurnRecord> RecentTurns { get; set; } = new();
}
