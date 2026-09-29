using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(ScoreCounter))]
public sealed class PatternEventScheduler : MonoBehaviour
{
    [SerializeField] private GameProgressionAsset gameProgression;
    [SerializeField] private ScoreCounter scoreCounter;
    [SerializeField] private bool logReachedEvents = true;

    private readonly List<ScheduledPatternEvent> scheduledEvents = new List<ScheduledPatternEvent>();
    private int nextEventIndex;
    private float lastObservedHeight;

    public event Action<ScheduledPatternEvent> EventReached;

    public void ResumeAtBound(int boundIndex, float height)
    {
        nextEventIndex = 0;
        while (nextEventIndex < scheduledEvents.Count &&
               (scheduledEvents[nextEventIndex].BoundIndex < boundIndex ||
                scheduledEvents[nextEventIndex].Height < height))
        {
            nextEventIndex++;
        }

        lastObservedHeight = height;
    }

    private void Awake()
    {
        if (scoreCounter == null)
        {
            scoreCounter = GetComponent<ScoreCounter>();
        }

        if (!TryBuildSchedule())
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (scoreCounter == null)
        {
            return;
        }

        float height = scoreCounter.CurrentScoreValue;
        if (height < lastObservedHeight)
        {
            nextEventIndex = 0;
        }

        lastObservedHeight = height;
        if (!scoreCounter.IsScoringActive)
        {
            return;
        }

        while (nextEventIndex < scheduledEvents.Count &&
               scheduledEvents[nextEventIndex].Height <= height)
        {
            ScheduledPatternEvent scheduledEvent = scheduledEvents[nextEventIndex++];
            EventReached?.Invoke(scheduledEvent);
            if (logReachedEvents)
            {
                Debug.Log($"PatternSchedule reached: {FormatEvent(scheduledEvent)}", this);
            }
        }
    }

    [ContextMenu("Log Pattern Schedule")]
    private void LogPatternSchedule()
    {
        if (scheduledEvents.Count == 0 && !TryBuildSchedule())
        {
            return;
        }

        for (int i = 0; i < scheduledEvents.Count; i++)
        {
            Debug.Log($"PatternSchedule planned: {FormatEvent(scheduledEvents[i])}", this);
        }
    }

    private bool TryBuildSchedule()
    {
        if (PatternEventSchedule.TryBuild(gameProgression, scheduledEvents, out string error))
        {
            return true;
        }

        Debug.LogError($"PatternEventScheduler: {error}", this);
        return false;
    }

    private static string FormatEvent(ScheduledPatternEvent scheduledEvent)
    {
        return $"bound {scheduledEvent.BoundIndex + 1}, " +
               $"pattern {scheduledEvent.PatternIndex + 1} (global {scheduledEvent.GlobalPatternIndex}, " +
               $"ID {scheduledEvent.PatternId}, coordinates from ID {scheduledEvent.CoordinateSourceId}), " +
               $"ball {scheduledEvent.EventIndex + 1}: {scheduledEvent.BalloonKind} at " +
               $"{scheduledEvent.Height:F2} m, angle {scheduledEvent.AngleDegrees:F1}°, " +
               $"radius {scheduledEvent.DistanceFromCenter:F3}";
    }
}
