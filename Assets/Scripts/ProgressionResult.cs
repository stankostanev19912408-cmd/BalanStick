using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerProgressData
{
    [SerializeField, Min(0)] private int currentLevel;

    public int CurrentLevel
    {
        get => Mathf.Max(0, currentLevel);
        set => currentLevel = Mathf.Max(0, value);
    }
}

public sealed class ProgressionResult
{
    private readonly List<UnlockedProgressionLevel> unlockedLevels = new List<UnlockedProgressionLevel>();

    public ProgressionResult(int previousLevel, int newLevel, int finalScore)
    {
        PreviousLevel = Mathf.Max(0, previousLevel);
        NewLevel = Mathf.Max(0, newLevel);
        FinalScore = Mathf.Max(0, finalScore);
    }

    public int PreviousLevel { get; }
    public int NewLevel { get; }
    public int FinalScore { get; }
    public bool LevelIncreased => NewLevel > PreviousLevel;
    public IReadOnlyList<UnlockedProgressionLevel> UnlockedLevels => unlockedLevels;

    public void AddUnlockedLevel(int levelNumber, ProgressionLevelDefinition definition)
    {
        unlockedLevels.Add(new UnlockedProgressionLevel(levelNumber, definition));
    }
}

public sealed class UnlockedProgressionLevel
{
    public UnlockedProgressionLevel(int levelNumber, ProgressionLevelDefinition definition)
    {
        LevelNumber = Mathf.Max(1, levelNumber);
        Definition = definition;
    }

    public int LevelNumber { get; }
    public ProgressionLevelDefinition Definition { get; }
}
