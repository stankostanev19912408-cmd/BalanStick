using System.Collections.Generic;
using UnityEngine;

public readonly struct ScheduledPatternEvent
{
    public ScheduledPatternEvent(
        int boundIndex,
        int patternIndex,
        int globalPatternIndex,
        int patternId,
        int coordinateSourceId,
        int eventIndex,
        float height,
        PatternBalloonKind balloonKind,
        float angleDegrees,
        float distanceFromCenter)
    {
        BoundIndex = boundIndex;
        PatternIndex = patternIndex;
        GlobalPatternIndex = globalPatternIndex;
        PatternId = patternId;
        CoordinateSourceId = coordinateSourceId;
        EventIndex = eventIndex;
        Height = height;
        BalloonKind = balloonKind;
        AngleDegrees = angleDegrees;
        DistanceFromCenter = distanceFromCenter;
    }

    public int BoundIndex { get; }
    public int PatternIndex { get; }
    public int GlobalPatternIndex { get; }
    public int PatternId { get; }
    public int CoordinateSourceId { get; }
    public int EventIndex { get; }
    public float Height { get; }
    public PatternBalloonKind BalloonKind { get; }
    public float AngleDegrees { get; }
    public float DistanceFromCenter { get; }
}

public static class PatternEventSchedule
{
    public static bool TryBuild(
        GameProgressionAsset progression,
        List<ScheduledPatternEvent> destination,
        out string error)
    {
        if (destination == null)
        {
            error = "Schedule destination is missing.";
            return false;
        }

        destination.Clear();
        if (progression == null)
        {
            error = "Game progression asset is missing.";
            return false;
        }

        if (!progression.TryValidate(out error))
        {
            return false;
        }

        int globalIndex = 0;
        for (int boundIndex = 0; boundIndex < progression.Bounds.Count; boundIndex++)
        {
            BoundAsset bound = progression.Bounds[boundIndex];
            float heightRange = bound.HeightTo - bound.HeightFrom;
            for (int patternIndex = 0; patternIndex < bound.PatternCount; patternIndex++, globalIndex++)
            {
                if (!progression.TryGetPatternAt(globalIndex, out PatternAsset current))
                {
                    destination.Clear();
                    error = $"Pattern at global index {globalIndex} is missing.";
                    return false;
                }

                int sourceId = globalIndex == 0
                    ? (current.PatternId == 1 ? 9 : current.PatternId - 1)
                    : progression.PatternSequence[globalIndex - 1];
                if (!progression.TryGetPatternById(sourceId, out PatternAsset coordinateSource))
                {
                    destination.Clear();
                    error = $"Coordinate source pattern ID {sourceId} is missing.";
                    return false;
                }

                for (int eventIndex = 0; eventIndex < PatternAsset.EventCount; eventIndex++)
                {
                    PatternSpawnEvent spawnEvent = current.SpawnEvents[eventIndex];
                    PatternRadialCoordinate coordinate = coordinateSource.Coordinates[eventIndex];
                    float fraction = (patternIndex + spawnEvent.LocalHeight) / bound.PatternCount;
                    float height = bound.HeightFrom + heightRange * fraction;

                    destination.Add(new ScheduledPatternEvent(
                        boundIndex,
                        patternIndex,
                        globalIndex,
                        current.PatternId,
                        sourceId,
                        eventIndex,
                        height,
                        spawnEvent.BalloonKind,
                        coordinate.AngleDegrees,
                        coordinate.DistanceFromCenter));
                }
            }
        }

        error = null;
        return true;
    }
}
