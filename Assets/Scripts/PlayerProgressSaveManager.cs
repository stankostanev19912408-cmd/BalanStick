using UnityEngine;

public class PlayerProgressSaveManager : MonoBehaviour
{
    private const string DefaultSaveKey = "player_progress";
    private const string HighestBoundKeySuffix = "_highest_bound";

    [SerializeField] private string saveKey = DefaultSaveKey;

    public PlayerProgressData LoadProgress()
    {
        string resolvedSaveKey = ResolveSaveKey();
        if (!PlayerPrefs.HasKey(resolvedSaveKey))
        {
            return new PlayerProgressData();
        }

        string json = PlayerPrefs.GetString(resolvedSaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new PlayerProgressData();
        }

        PlayerProgressData loadedData = JsonUtility.FromJson<PlayerProgressData>(json);
        return loadedData ?? new PlayerProgressData();
    }

    public void SaveProgress(PlayerProgressData progressData)
    {
        PlayerProgressData dataToSave = progressData ?? new PlayerProgressData();
        string json = JsonUtility.ToJson(dataToSave);
        PlayerPrefs.SetString(ResolveSaveKey(), json);
        PlayerPrefs.Save();
    }

    public void DeleteProgress()
    {
        PlayerPrefs.DeleteKey(ResolveSaveKey());
        PlayerPrefs.Save();
    }

    public int LoadHighestReachedBoundIndex()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(ResolveSaveKey() + HighestBoundKeySuffix, 0));
    }

    public void SaveHighestReachedBoundIndex(int boundIndex)
    {
        int highestBoundIndex = Mathf.Max(0, boundIndex);
        if (highestBoundIndex <= LoadHighestReachedBoundIndex())
        {
            return;
        }

        PlayerPrefs.SetInt(ResolveSaveKey() + HighestBoundKeySuffix, highestBoundIndex);
        PlayerPrefs.Save();
    }

    public void DeleteHighestReachedBoundIndex()
    {
        PlayerPrefs.DeleteKey(ResolveSaveKey() + HighestBoundKeySuffix);
        PlayerPrefs.Save();
    }

    private string ResolveSaveKey()
    {
        if (string.IsNullOrWhiteSpace(saveKey))
        {
            saveKey = DefaultSaveKey;
        }

        return saveKey;
    }
}
