using UnityEngine;

public class PlayerProgressSaveManager : MonoBehaviour
{
    private const string DefaultSaveKey = "player_progress";
    private const string HighestBoundKeySuffix = "_highest_bound";
    private const string CurrencyKeySuffix = "_currency";

    [SerializeField] private string saveKey = DefaultSaveKey;

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

    public int LoadCurrencyBalance()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(ResolveSaveKey() + CurrencyKeySuffix, 0));
    }

    public int AddCurrency(int amount)
    {
        int balance = LoadCurrencyBalance();
        if (amount <= 0)
        {
            return balance;
        }

        long total = (long)balance + amount;
        int newBalance = total >= int.MaxValue ? int.MaxValue : (int)total;
        PlayerPrefs.SetInt(ResolveSaveKey() + CurrencyKeySuffix, newBalance);
        PlayerPrefs.Save();
        return newBalance;
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
