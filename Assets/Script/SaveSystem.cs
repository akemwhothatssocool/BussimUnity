using System;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public string sceneName = "GameScene";
    public int currentDay = 1;
    public int stopsReached = 0;
    public int stopsPerDay = 5;
    public int dailyPassengers = 0;
    public int dailyMissed = 0;
    public int dailyIncome = 0;
    public int dailyGasCost = 300;
    public int dailyRepairCost = 150;
    public float engineSpeedBonus = 0f;
    public float permanentPopularityBonus = 0f;
    public float popularity = 50f;
    public float dailyPopularityGain = 0f;
    public int playerMoney = 100;
    public int engineUpgradeCost = 300;
    public int fuelUpgradeCost = 300;
    public int seatUpgradeCost = 300;
    public SeatSaveData[] seatStates = Array.Empty<SeatSaveData>();
}

[Serializable]
public class SeatSaveData
{
    public string seatId = string.Empty;
    public int state = 0;
    public int level = 0;
}

public static class SaveSystem
{
    private const string SaveKey = "GameSaveData";
    private const string PlayerMoneyKey = "PlayerMoney";

    private static bool pendingContinueLoad = false;

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey(PlayerMoneyKey);
        PlayerPrefs.Save();
        pendingContinueLoad = false;
    }

    public static void PrepareContinue()
    {
        pendingContinueLoad = HasSave();
    }

    public static bool ShouldLoadOnSceneEnter()
    {
        if (!pendingContinueLoad) return false;

        pendingContinueLoad = false;
        return HasSave();
    }

    public static bool TryLoad(out GameSaveData data)
    {
        data = null;
        if (!HasSave()) return false;

        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return false;

        data = JsonUtility.FromJson<GameSaveData>(json);
        return data != null;
    }

    public static string GetSavedSceneName(string fallbackScene)
    {
        if (!TryLoad(out GameSaveData data))
            return fallbackScene;

        return string.IsNullOrWhiteSpace(data.sceneName) ? fallbackScene : data.sceneName;
    }

    public static void SaveCurrentGame()
    {
        if (GameManager.Instance == null) return;

        GameSaveData data = new GameSaveData
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            currentDay = GameManager.Instance.currentDay,
            stopsReached = GameManager.Instance.stopsReached,
            stopsPerDay = GameManager.Instance.stopsPerDay,
            dailyPassengers = GameManager.Instance.dailyPassengers,
            dailyMissed = GameManager.Instance.dailyMissed,
            dailyIncome = GameManager.Instance.dailyIncome,
            dailyGasCost = GameManager.Instance.dailyGasCost,
            dailyRepairCost = GameManager.Instance.dailyRepairCost,
            engineSpeedBonus = GameManager.Instance.engineSpeedBonus,
            permanentPopularityBonus = GameManager.Instance.permanentPopularityBonus,
            popularity = GameManager.Instance.popularity,
            dailyPopularityGain = GameManager.Instance.dailyPopularityGain,
            playerMoney = PlayerWallet.Instance != null ? PlayerWallet.Instance.currentMoney : PlayerPrefs.GetInt(PlayerMoneyKey, 100)
        };

        if (UpgradeManager.Instance != null)
        {
            data.engineUpgradeCost = UpgradeManager.Instance.engineUpgradeCost;
            data.fuelUpgradeCost = UpgradeManager.Instance.fuelUpgradeCost;
            data.seatUpgradeCost = UpgradeManager.Instance.seatUpgradeCost;
        }

        BusSeat[] seats = UnityEngine.Object.FindObjectsByType<BusSeat>(FindObjectsSortMode.None);
        data.seatStates = new SeatSaveData[seats.Length];
        for (int i = 0; i < seats.Length; i++)
        {
            data.seatStates[i] = seats[i] != null ? seats[i].CaptureSaveData() : null;
        }

        WriteSave(data);
    }

    public static void WriteSave(GameSaveData data)
    {
        if (data == null) return;

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.SetInt(PlayerMoneyKey, data.playerMoney);
        PlayerPrefs.Save();
    }
}
