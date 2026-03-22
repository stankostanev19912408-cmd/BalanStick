using UnityEngine;

public class PlayerProgressSaveManager : MonoBehaviour
{
    private const string DefaultSaveKey = "player_progress";

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

    private string ResolveSaveKey()
    {
        if (string.IsNullOrWhiteSpace(saveKey))
        {
            saveKey = DefaultSaveKey;
        }

        return saveKey;
    }
}
