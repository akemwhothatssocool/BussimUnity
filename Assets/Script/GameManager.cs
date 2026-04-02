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

    [Header("=== Debug ===")]
    public bool alwaysStartFreshInEditor = true;

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

        bool forceFreshStart = ShouldForceFreshStart();
        if (forceFreshStart)
            SaveSystem.DeleteSave();

        SeatPlacementMarker.EnsureAllGeneratedSlots();
        RandomEventManager.GetOrCreateInstance();

        if (!forceFreshStart && SaveSystem.ShouldLoadOnSceneEnter() && SaveSystem.TryLoad(out GameSaveData saveData))
        {
            ApplySaveData(saveData);
        }
        else
        {
            if (PlayerWallet.Instance != null)
                PlayerWallet.Instance.ResetToStartingMoney(false);

            InitializeSeatMarkersForNewGame();
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

    public float GetStarRating()
    {
        float finalPopularity = Mathf.Clamp(popularity + permanentPopularityBonus, 0f, 100f);
        return finalPopularity / 20f;
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
            float starRating = GetStarRating();
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
        BusStopManager busStopManager = Object.FindFirstObjectByType<BusStopManager>();
        if (busStopManager != null)
            busStopManager.ResetForNextDay();

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
        SeatPlacementMarker.EnsureAllGeneratedSlots();

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

        if (!HasSavedLayoutSeatState(data.seatStates))
            InitializeSeatMarkersForNewGame();

        SeatDeliveryManager.GetOrCreateInstance().ApplySaveData(data);

        CityManager cityManager = Object.FindFirstObjectByType<CityManager>();
        if (cityManager != null)
            cityManager.ApplyEngineSpeedBonus(engineSpeedBonus);

        if (busRateDisplay != null)
        {
            busRateDisplay.UpdateBusRate(GetStarRating());
        }
    }

    void InitializeSeatMarkersForNewGame()
    {
        SeatPlacementMarker.EnsureAllGeneratedSlots();

        SeatPlacementMarker[] allMarkers = Object.FindObjectsByType<SeatPlacementMarker>(FindObjectsSortMode.None);
        if (allMarkers == null || allMarkers.Length == 0)
            return;

        System.Collections.Generic.List<SeatPlacementMarker> markers = new System.Collections.Generic.List<SeatPlacementMarker>(allMarkers.Length);
        for (int i = 0; i < allMarkers.Length; i++)
        {
            if (allMarkers[i] != null && allMarkers[i].CountsAsSeatSlot)
                markers.Add(allMarkers[i]);
        }

        if (markers.Count == 0)
            return;

        markers = BuildDistributedMarkerOrder(markers);

        HashSet<int> usableIndices = BuildSpreadIndexSet(markers.Count, 3);
        HashSet<int> brokenIndices = BuildSpreadIndexSet(markers.Count, 5, usableIndices);

        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] == null)
                continue;

            BusSeat seat = markers[i].GetComponent<BusSeat>();
            if (seat == null)
                continue;

            seat.sellPrice = 50;

            if (usableIndices.Contains(i))
            {
                seat.currentState = BusSeat.SeatState.Usable;
                seat.currentLevel = BusSeat.SeatLevel.Lv1;
            }
            else if (brokenIndices.Contains(i))
            {
                seat.currentState = BusSeat.SeatState.Broken;
                seat.currentLevel = BusSeat.SeatLevel.None;
            }
            else
            {
                seat.currentState = BusSeat.SeatState.Empty;
                seat.currentLevel = BusSeat.SeatLevel.None;
            }

            seat.UpdateVisuals();
        }

        Debug.Log($"Seat layout initialized: {markers.Count} slots | usable: {Mathf.Min(3, markers.Count)} | broken: {Mathf.Clamp(markers.Count - 3, 0, 5)} | empty: {Mathf.Max(0, markers.Count - 8)}");
    }

    List<SeatPlacementMarker> BuildDistributedMarkerOrder(List<SeatPlacementMarker> markers)
    {
        List<SeatPlacementMarker> ordered = new List<SeatPlacementMarker>(markers);
        ordered.Sort((a, b) =>
        {
            if (a == null || b == null)
                return string.CompareOrdinal(BuildMarkerSortKey(a), BuildMarkerSortKey(b));

            Vector3 positionA = a.transform.localPosition;
            Vector3 positionB = b.transform.localPosition;

            int rowCompare = (-positionA.z).CompareTo(-positionB.z);
            if (rowCompare != 0)
                return rowCompare;

            return positionA.x.CompareTo(positionB.x);
        });

        List<List<SeatPlacementMarker>> rows = new List<List<SeatPlacementMarker>>();
        const float rowTolerance = 0.35f;

        for (int i = 0; i < ordered.Count; i++)
        {
            SeatPlacementMarker marker = ordered[i];
            if (marker == null)
                continue;

            if (rows.Count == 0)
            {
                rows.Add(new List<SeatPlacementMarker> { marker });
                continue;
            }

            List<SeatPlacementMarker> currentRow = rows[rows.Count - 1];
            float rowZ = currentRow[0].transform.localPosition.z;
            if (Mathf.Abs(marker.transform.localPosition.z - rowZ) <= rowTolerance)
            {
                currentRow.Add(marker);
            }
            else
            {
                rows.Add(new List<SeatPlacementMarker> { marker });
            }
        }

        List<SeatPlacementMarker> result = new List<SeatPlacementMarker>(ordered.Count);
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            List<SeatPlacementMarker> row = rows[rowIndex];
            row.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

            if (rowIndex % 2 == 1)
                row.Reverse();

            result.AddRange(row);
        }

        return result;
    }

    HashSet<int> BuildSpreadIndexSet(int totalCount, int desiredCount, HashSet<int> excluded = null)
    {
        HashSet<int> result = new HashSet<int>();
        if (totalCount <= 0 || desiredCount <= 0)
            return result;

        List<int> available = new List<int>(totalCount);
        for (int i = 0; i < totalCount; i++)
        {
            if (excluded == null || !excluded.Contains(i))
                available.Add(i);
        }

        if (available.Count == 0)
            return result;

        int count = Mathf.Min(desiredCount, available.Count);
        if (count == available.Count)
        {
            for (int i = 0; i < available.Count; i++)
                result.Add(available[i]);

            return result;
        }

        if (count == 1)
        {
            result.Add(available[available.Count / 2]);
            return result;
        }

        for (int pick = 0; pick < count; pick++)
        {
            float normalized = (pick + 0.5f) / count;
            int candidateIndex = Mathf.Clamp(Mathf.RoundToInt(normalized * available.Count - 0.5f), 0, available.Count - 1);

            while (candidateIndex < available.Count && result.Contains(available[candidateIndex]))
                candidateIndex++;

            while (candidateIndex >= 0 && candidateIndex < available.Count && result.Contains(available[candidateIndex]))
                candidateIndex--;

            candidateIndex = Mathf.Clamp(candidateIndex, 0, available.Count - 1);
            result.Add(available[candidateIndex]);
        }

        return result;
    }

    string BuildMarkerSortKey(SeatPlacementMarker marker)
    {
        if (marker == null)
            return string.Empty;

        return marker.transform.GetSiblingIndex().ToString("D4") + "_" + BuildTransformPath(marker.transform);
    }

    bool HasSavedLayoutSeatState(SeatSaveData[] seatStates)
    {
        if (seatStates == null || seatStates.Length == 0)
            return false;

        for (int i = 0; i < seatStates.Length; i++)
        {
            if (seatStates[i] == null || string.IsNullOrEmpty(seatStates[i].seatId))
                continue;

            if (seatStates[i].seatId.Contains("SeatPlacementSlot_"))
                return true;
        }

        return false;
    }

    bool ShouldForceFreshStart()
    {
#if UNITY_EDITOR
        return alwaysStartFreshInEditor;
#else
        return false;
#endif
    }

    string BuildTransformPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
