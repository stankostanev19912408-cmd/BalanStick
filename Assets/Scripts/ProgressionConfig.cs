using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Game/Progression Config")]
public class ProgressionConfig : ScriptableObject
{
    [SerializeField] private List<ProgressionLevelDefinition> levels = new List<ProgressionLevelDefinition>();

    public IReadOnlyList<ProgressionLevelDefinition> Levels => levels;

    public bool HasLevels => levels != null && levels.Count > 0;

    public int GetReachedLevel(int score)
    {
        if (!HasLevels)
        {
            return 0;
        }

        score = Mathf.Max(0, score);

        int reachedLevel = 0;
        for (int i = 0; i < levels.Count; i++)
        {
            if (score < levels[i].RequiredScore)
            {
                break;
            }

            reachedLevel = i + 1;
        }

        return reachedLevel;
    }

    public ProgressionLevelDefinition GetLevelDefinition(int levelNumber)
    {
        if (!HasLevels || levelNumber <= 0 || levelNumber > levels.Count)
        {
            return null;
        }

        return levels[levelNumber - 1];
    }

    private void OnValidate()
    {
        if (levels == null)
        {
            levels = new List<ProgressionLevelDefinition>();
            return;
        }

        foreach (ProgressionLevelDefinition level in levels)
        {
            if (level == null)
            {
                continue;
            }

            level.ClampValues();
        }

        levels.Sort(CompareLevels);
    }

    private static int CompareLevels(ProgressionLevelDefinition left, ProgressionLevelDefinition right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.RequiredScore.CompareTo(right.RequiredScore);
    }
}

[Serializable]
public class ProgressionLevelDefinition
{
    [SerializeField, Min(0)] private int requiredScore;
    [SerializeField] private string title;
    [SerializeField, TextArea] private string description;
    [SerializeField, Min(0)] private int softCurrencyReward;
    [SerializeField] private string[] unlockedFeatureIds = Array.Empty<string>();

    public int RequiredScore => requiredScore;
    public string Title => title;
    public string Description => description;
    public int SoftCurrencyReward => softCurrencyReward;
    public IReadOnlyList<string> UnlockedFeatureIds => unlockedFeatureIds;

    public void ClampValues()
    {
        requiredScore = Mathf.Max(0, requiredScore);
        softCurrencyReward = Mathf.Max(0, softCurrencyReward);

        if (unlockedFeatureIds == null)
        {
            unlockedFeatureIds = Array.Empty<string>();
        }
    }
}
