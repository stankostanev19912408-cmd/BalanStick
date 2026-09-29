using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "GameProgression", menuName = "BalanStick/Progression/Game Progression")]
public sealed class GameProgressionAsset : ScriptableObject
{
    [SerializeField] private List<BoundAsset> bounds = new List<BoundAsset>();
    [SerializeField] private string patternSequenceDigits = string.Empty;
    [SerializeField] private List<PatternAsset> patternCatalog = new List<PatternAsset>();

    private int[] parsedPatternSequence = Array.Empty<int>();
    private string cachedSequenceDigits;
    private int cachedPatternCount = -1;
    private string sequenceParseError;

    public IReadOnlyList<BoundAsset> Bounds => bounds;
    public IReadOnlyList<int> PatternSequence
    {
        get
        {
            EnsureSequenceCache();
            return parsedPatternSequence;
        }
    }
    public IReadOnlyList<PatternAsset> PatternCatalog => patternCatalog;

    public int TotalPatternCount
    {
        get
        {
            if (bounds == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < bounds.Count; i++)
            {
                if (bounds[i] != null)
                {
                    count += Mathf.Max(0, bounds[i].PatternCount);
                }
            }

            return count;
        }
    }

    public bool TryGetPatternAt(int globalIndex, out PatternAsset pattern)
    {
        pattern = null;
        EnsureSequenceCache();
        if (globalIndex < 0 || globalIndex >= parsedPatternSequence.Length || sequenceParseError != null)
        {
            return false;
        }

        return TryGetPatternById(parsedPatternSequence[globalIndex], out pattern);
    }

    public bool TryGetPatternById(int patternId, out PatternAsset pattern)
    {
        pattern = null;
        if (patternId < 1 || patternId > 9 || patternCatalog == null)
        {
            return false;
        }

        for (int i = 0; i < patternCatalog.Count; i++)
        {
            PatternAsset candidate = patternCatalog[i];
            if (candidate == null || candidate.PatternId != patternId)
            {
                continue;
            }

            if (pattern != null && pattern != candidate)
            {
                pattern = null;
                return false;
            }

            pattern = candidate;
        }

        return pattern != null;
    }

    public bool TryValidate(out string errors)
    {
        StringBuilder report = new StringBuilder();
        PatternAsset[] patternsById = new PatternAsset[10];
        ValidateCatalog(report, patternsById);
        ValidateBounds(report);
        ValidateSequence(report, patternsById);

        errors = report.ToString();
        return errors.Length == 0;
    }

    private void OnEnable()
    {
        RebuildSequenceCache();
    }

    private void OnValidate()
    {
        RebuildSequenceCache();
        if (!TryValidate(out string errors))
        {
            Debug.LogError($"GameProgressionAsset '{name}' is invalid:\n{errors}", this);
        }
    }

    private void ValidateCatalog(StringBuilder report, PatternAsset[] patternsById)
    {
        if (patternCatalog == null)
        {
            AddError(report, "Pattern catalog is missing.");
            return;
        }

        for (int i = 0; i < patternCatalog.Count; i++)
        {
            PatternAsset pattern = patternCatalog[i];
            if (pattern == null)
            {
                AddError(report, $"Pattern catalog entry {i} is empty.");
                continue;
            }

            int id = pattern.PatternId;
            if (id < 1 || id > 9)
            {
                AddError(report, $"Pattern '{pattern.name}' has ID {id}; expected 1..9.");
                continue;
            }

            if (patternsById[id] != null)
            {
                AddError(report, $"Pattern ID {id} occurs more than once in the catalog.");
                continue;
            }

            patternsById[id] = pattern;
            ValidatePattern(report, pattern);
        }

        for (int id = 1; id <= 9; id++)
        {
            if (patternsById[id] == null)
            {
                AddError(report, $"Pattern catalog has no asset for ID {id}.");
            }
        }
    }

    private void ValidateSequence(StringBuilder report, PatternAsset[] patternsById)
    {
        EnsureSequenceCache();
        if (sequenceParseError != null)
        {
            AddError(report, sequenceParseError);
        }

        if (parsedPatternSequence.Length < TotalPatternCount)
        {
            AddError(report, "Pattern sequence has fewer nonzero digits than the total pattern count of all bounds.");
        }

        for (int i = 0; i < parsedPatternSequence.Length; i++)
        {
            int id = parsedPatternSequence[i];
            if (id < 1 || id > 9 || patternsById[id] == null)
            {
                AddError(report, $"Pattern sequence entry {i} has no valid pattern asset for ID {id}.");
            }
        }
    }

    private void EnsureSequenceCache()
    {
        if (cachedPatternCount != TotalPatternCount ||
            !string.Equals(cachedSequenceDigits, patternSequenceDigits, StringComparison.Ordinal))
        {
            RebuildSequenceCache();
        }
    }

    private void RebuildSequenceCache()
    {
        int requiredCount = TotalPatternCount;
        cachedPatternCount = requiredCount;
        cachedSequenceDigits = patternSequenceDigits;
        sequenceParseError = null;

        if (patternSequenceDigits == null)
        {
            parsedPatternSequence = Array.Empty<int>();
            sequenceParseError = "Pattern sequence string is missing.";
            return;
        }

        List<int> parsed = new List<int>(requiredCount);
        for (int i = 0; i < patternSequenceDigits.Length; i++)
        {
            char digit = patternSequenceDigits[i];
            if (digit < '0' || digit > '9')
            {
                sequenceParseError = $"Pattern sequence contains a non-digit character at index {i}.";
                break;
            }

            if (digit != '0' && parsed.Count < requiredCount)
            {
                parsed.Add(digit - '0');
            }
        }

        parsedPatternSequence = parsed.ToArray();
    }

    private void ValidateBounds(StringBuilder report)
    {
        if (bounds == null || bounds.Count == 0)
        {
            AddError(report, "At least one bound is required.");
            return;
        }

        BoundAsset previousBound = null;
        for (int boundIndex = 0; boundIndex < bounds.Count; boundIndex++)
        {
            BoundAsset bound = bounds[boundIndex];
            if (bound == null)
            {
                AddError(report, $"Bound entry {boundIndex} is empty.");
                previousBound = null;
                continue;
            }

            if (!IsFinite(bound.HeightFrom) || !IsFinite(bound.HeightTo) ||
                bound.HeightFrom < 0f || bound.HeightTo <= bound.HeightFrom)
            {
                AddError(report, $"Bound '{bound.name}' has an invalid height range.");
            }

            if (previousBound != null && !Mathf.Approximately(previousBound.HeightTo, bound.HeightFrom))
            {
                AddError(report, $"Bounds '{previousBound.name}' and '{bound.name}' do not meet at the same height.");
            }

            if (!IsFinite(bound.GrowthSpeedMultiplier) || bound.GrowthSpeedMultiplier <= 0f)
            {
                AddError(report, $"Bound '{bound.name}' needs a positive growth speed multiplier.");
            }

            if (bound.PatternCount <= 0)
            {
                AddError(report, $"Bound '{bound.name}' needs a positive pattern count.");
            }

            previousBound = bound;
        }
    }

    private static void ValidatePattern(StringBuilder report, PatternAsset pattern)
    {
        IReadOnlyList<PatternSpawnEvent> events = pattern.SpawnEvents;
        IReadOnlyList<PatternRadialCoordinate> coordinates = pattern.Coordinates;
        if (events == null || events.Count != PatternAsset.EventCount ||
            coordinates == null || coordinates.Count != PatternAsset.EventCount)
        {
            AddError(report, $"Pattern '{pattern.name}' needs exactly nine events and nine coordinates.");
            return;
        }

        float previousHeight = 0f;
        for (int i = 0; i < PatternAsset.EventCount; i++)
        {
            PatternSpawnEvent spawnEvent = events[i];
            float height = spawnEvent.LocalHeight;
            if (!IsFinite(height) || height < 0f || height > 1f || height < previousHeight)
            {
                AddError(report, $"Pattern '{pattern.name}' has an invalid event height at index {i}.");
            }

            if (!Enum.IsDefined(typeof(PatternBalloonKind), spawnEvent.BalloonKind))
            {
                AddError(report, $"Pattern '{pattern.name}' has an invalid balloon kind at index {i}.");
            }

            PatternRadialCoordinate coordinate = coordinates[i];
            if (!IsFinite(coordinate.AngleDegrees) || coordinate.AngleDegrees < 0f || coordinate.AngleDegrees >= 360f ||
                !IsFinite(coordinate.DistanceFromCenter) ||
                coordinate.DistanceFromCenter < PatternAsset.MinSpawnDistance ||
                coordinate.DistanceFromCenter > PatternAsset.MaxSpawnDistance)
            {
                AddError(report, $"Pattern '{pattern.name}' has an invalid coordinate at index {i}.");
            }

            previousHeight = height;
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static void AddError(StringBuilder report, string message)
    {
        report.Append("- ").AppendLine(message);
    }
}
