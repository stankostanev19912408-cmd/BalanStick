using System;
using System.Collections.Generic;
using UnityEngine;

public enum PatternBalloonKind
{
    Red,
    Orange,
    Yellow,
    Green,
    Cyan,
    Blue,
    Violet,
    Black,
    White,
    Gray
}

[Serializable]
public struct PatternSpawnEvent
{
    [SerializeField, Range(0f, 1f)] private float localHeight;
    [SerializeField] private PatternBalloonKind balloonKind;

    public float LocalHeight => localHeight;
    public PatternBalloonKind BalloonKind => balloonKind;
}

[Serializable]
public struct PatternRadialCoordinate
{
    [SerializeField, Range(0f, 360f)] private float angleDegrees;
    [SerializeField, Range(PatternAsset.MinSpawnDistance, PatternAsset.MaxSpawnDistance)]
    private float distanceFromCenter;

    public float AngleDegrees => angleDegrees;
    public float DistanceFromCenter => distanceFromCenter;

    public void ClampToSpawnArea()
    {
        angleDegrees = Mathf.Repeat(angleDegrees, 360f);
        distanceFromCenter = Mathf.Clamp(
            distanceFromCenter,
            PatternAsset.MinSpawnDistance,
            PatternAsset.MaxSpawnDistance);
    }
}

[CreateAssetMenu(fileName = "Pattern", menuName = "BalanStick/Progression/Pattern")]
public sealed class PatternAsset : ScriptableObject
{
    public const int EventCount = 9;
    public const float MinSpawnDistance = 0.22f;
    public const float MaxSpawnDistance = 0.33f;

    [SerializeField, Range(1, 9)] private int patternId = 1;
    [SerializeField] private PatternSpawnEvent[] spawnEvents = new PatternSpawnEvent[EventCount];
    [SerializeField] private PatternRadialCoordinate[] coordinates = new PatternRadialCoordinate[EventCount];

    public int PatternId => patternId;
    public IReadOnlyList<PatternSpawnEvent> SpawnEvents => spawnEvents;
    public IReadOnlyList<PatternRadialCoordinate> Coordinates => coordinates;

    private void OnValidate()
    {
        if (spawnEvents == null || spawnEvents.Length != EventCount)
        {
            Array.Resize(ref spawnEvents, EventCount);
        }

        if (coordinates == null || coordinates.Length != EventCount)
        {
            Array.Resize(ref coordinates, EventCount);
        }

        for (int i = 0; i < coordinates.Length; i++)
        {
            PatternRadialCoordinate coordinate = coordinates[i];
            coordinate.ClampToSpawnArea();
            coordinates[i] = coordinate;
        }

        float previousHeight = 0f;
        for (int i = 0; i < spawnEvents.Length; i++)
        {
            float height = spawnEvents[i].LocalHeight;
            if (float.IsNaN(height) || float.IsInfinity(height) ||
                height < 0f || height > 1f || height < previousHeight)
            {
                Debug.LogError($"Pattern '{name}' has an invalid event height at index {i}.", this);
            }

            if (!Enum.IsDefined(typeof(PatternBalloonKind), spawnEvents[i].BalloonKind))
            {
                Debug.LogError($"Pattern '{name}' has an invalid balloon kind at index {i}.", this);
            }

            previousHeight = height;
        }
    }
}
