using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("=== ระบบวันและป้าย ===")]
    public int currentDay = 1;
    public int stopsReached = 0;
    public int stopsPerDay = 5;

    [Header("=== สถิติประจำวัน (Daily Stats) ===")]
    public int dailyPassengers = 0;
    public int dailyMissed = 0;

    [Header("=== ระบบการเงินรายวัน ===")]
    public int dailyIncome = 0;
    public int dailyGasCost = 300;
    public int dailyRepairCost = 150;
    // ❌ เอา public int totalMoney ออกไปเลย เพราะเราใช้ PlayerWallet แทนแล้ว 100%

    [Header("=== โบนัสอัปเกรด (Upgrade Stats) ===")]
    public float engineSpeedBonus = 0f; // 🌟 เตรียมไว้สำหรับอัปเกรดความเร็วรถ
    public float permanentPopularityBonus = 0f;

    [Header("=== ระบบความนิยม ===")]
    [Tooltip("ความนิยม 0-100% (จะถูกแปลงเป็นดาว 0-5 ดวง)")]
    public float popularity = 50f;
    public float dailyPopularityGain = 12f;

    [Header("=== UI สรุปผลจบวัน (New UI) ===")]
    public GameObject summaryPanel;

    [Space(10)]
    public TextMeshProUGUI txtPassengers;
    public TextMeshProUGUI txtStops;
    public TextMeshProUGUI txtMissed;

    [Space(10)]
    public TextMeshProUGUI txtTotalIncome;
    public TextMeshProUGUI txtFuelCost;
    public TextMeshProUGUI txtRepairCost;
    public TextMeshProUGUI txtNetProfit;
    public TextMeshProUGUI txtPopularityGain;

    [Header("=== สคริปต์ดาว ===")]
    public BusRateDisplay busRateDisplay;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);
        StartCoroutine(InitializeGameState());
    }

    IEnumerator InitializeGameState()
    {
        yield return null;

        if (SaveSystem.ShouldLoadOnSceneEnter() && SaveSystem.TryLoad(out GameSaveData saveData))
        {
            ApplySaveData(saveData);
        }
        else
        {
            SaveSystem.SaveCurrentGame();
        }
    }

    public void AddPassenger()
    {
        dailyPassengers++;
        SaveSystem.SaveCurrentGame();
    }

    public void AddMissedPassenger()
    {
        dailyMissed++;
        SaveSystem.SaveCurrentGame();
    }

    public void AddDailyIncome(int amount)
    {
        dailyIncome += amount;
        SaveSystem.SaveCurrentGame();
    }

    public void AddStop()
    {
        if (stopsReached >= stopsPerDay) return;

        stopsReached++;
        Debug.Log($"ป้ายที่ {stopsReached} / {stopsPerDay}");

        if (stopsReached >= stopsPerDay)
        {
            EndDay();
        }
        else
        {
            SaveSystem.SaveCurrentGame();
        }
    }

    public void AdjustPopularity(float amount)
    {
        dailyPopularityGain += amount;
        popularity = Mathf.Clamp(popularity + amount, 0f, 100f);
        SaveSystem.SaveCurrentGame();
    }

    // ==========================================
    // 🌟 จบวัน: สรุปยอด และ อัปเดต UI แบบจัดเต็ม
    // ==========================================
    [ContextMenu("Test End Day")]
    public void EndDay()
    {
        FareSystem fare = Object.FindFirstObjectByType<FareSystem>();
        if (fare != null) fare.ForceResetSystem();

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 🌟 คำนวณเงินสุทธิ
        int netProfit = dailyIncome - dailyGasCost - dailyRepairCost;

        // 🌟 หักค่าใช้จ่ายรายวันออกจากกระเป๋าตังค์ (เพราะรายได้ถูกบวกไปแล้วแบบ Real-time ตอนเก็บค่าตั๋ว)
        if (PlayerWallet.Instance != null)
        {
            int dailyExpenses = dailyGasCost + dailyRepairCost;
            PlayerWallet.Instance.AddMoney(-dailyExpenses);
        }

        // โยนข้อมูลใส่ Text UI
        if (txtPassengers) txtPassengers.text = dailyPassengers.ToString();
        if (txtStops) txtStops.text = stopsReached.ToString();
        if (txtMissed) txtMissed.text = dailyMissed.ToString();

        if (txtTotalIncome) txtTotalIncome.text = $"+{dailyIncome}";
        if (txtFuelCost) txtFuelCost.text = $"-{dailyGasCost}";
        if (txtRepairCost) txtRepairCost.text = $"-{dailyRepairCost}";

        if (txtNetProfit)
        {
            if (netProfit >= 0) txtNetProfit.text = $"+{netProfit}";
            else txtNetProfit.text = $"{netProfit}";
        }

        if (txtPopularityGain)
        {
            if (dailyPopularityGain >= 0) txtPopularityGain.text = $"+{dailyPopularityGain}";
            else txtPopularityGain.text = $"{dailyPopularityGain}";
        }

        // อัปเดตดาว
        if (busRateDisplay != null)
        {
            float finalPopularity = Mathf.Clamp(popularity + permanentPopularityBonus, 0f, 100f);
            float starRating = finalPopularity / 20f;
            busRateDisplay.UpdateBusRate(starRating);
            Debug.Log($"⭐ BusRate: {starRating} ดาว (คะแนนดิบ: {popularity}% + โบนัสเบาะ: {permanentPopularityBonus}%)");
        }

        if (summaryPanel != null) summaryPanel.SetActive(true);

        SaveSystem.SaveCurrentGame();
    }

    // ==========================================
    // เริ่มวันใหม่: ล้างคนเก่า ล้างบั๊ก ล้างสถิติรายวัน
    // ==========================================
    public void StartNextDay()
    {
        currentDay++;
        stopsReached = 0;
        dailyIncome = 0;
        dailyPassengers = 0;
        dailyMissed = 0;
        dailyPopularityGain = 0;

        FareSystem fare = Object.FindFirstObjectByType<FareSystem>();
        if (fare != null) fare.ForceResetSystem();

        PassengerAI[] remainingPassengers = Object.FindObjectsByType<PassengerAI>(FindObjectsSortMode.None);
        foreach (PassengerAI p in remainingPassengers)
        {
            Destroy(p.gameObject);
        }

        if (summaryPanel != null) summaryPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("เริ่มวันใหม่! ลุยเก็บเงินสร้างตัว!");
        SaveSystem.SaveCurrentGame();
    }

    public void OpenUpgradeMenu()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OpenMenu();
        }
    }

    public void ReturnToMainMenu()
    {
        SaveSystem.SaveCurrentGame();
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    void OnApplicationQuit()
    {
        SaveSystem.SaveCurrentGame();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveSystem.SaveCurrentGame();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveSystem.SaveCurrentGame();
    }

    void ApplySaveData(GameSaveData data)
    {
        currentDay = data.currentDay;
        stopsReached = data.stopsReached;
        stopsPerDay = data.stopsPerDay;
        dailyPassengers = data.dailyPassengers;
        dailyMissed = data.dailyMissed;
        dailyIncome = data.dailyIncome;
        dailyGasCost = data.dailyGasCost;
        dailyRepairCost = data.dailyRepairCost;
        engineSpeedBonus = data.engineSpeedBonus;
        permanentPopularityBonus = data.permanentPopularityBonus;
        popularity = data.popularity;
        dailyPopularityGain = data.dailyPopularityGain;

        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.SetMoney(data.playerMoney, false);

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ApplySaveData(data);

        BusSeat.ApplySavedSeats(data.seatStates);

        if (busRateDisplay != null)
        {
            float finalPopularity = Mathf.Clamp(popularity + permanentPopularityBonus, 0f, 100f);
            busRateDisplay.UpdateBusRate(finalPopularity / 20f);
        }
    }
}
