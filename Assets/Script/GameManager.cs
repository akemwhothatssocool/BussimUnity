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

    [Header("=== ระบบการเงินรายวัน ===")]
    public int dailyIncome = 0;
    public int dailyGasCost = 150;
    public int totalMoney = 100; // 🌟 เริ่มต้นด้วยทุนมรดก 100 บาท

    [Header("=== ระบบความนิยม ===")]
    [Tooltip("ความนิยม 0-100% มีผลกับจำนวนคนขึ้นรถ")]
    public float popularity = 50f;

    [Header("=== UI สรุปผลจบวัน ===")]
    public GameObject summaryPanel;
    public TextMeshProUGUI txtDay;
    public TextMeshProUGUI txtIncome;
    public TextMeshProUGUI txtExpense;
    public TextMeshProUGUI txtProfit;
    public TextMeshProUGUI txtPopularity;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);

        // ถ้าอยากให้เงินบันทึกข้ามการปิดเกม ให้ใช้ PlayerPrefs (ทางเลือก)
        // totalMoney = PlayerPrefs.GetInt("TotalMoney", 100);
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
    }

    public void AddDailyIncome(int amount)
    {
        dailyIncome += amount;
    }

    public void AdjustPopularity(float amount)
    {
        popularity = Mathf.Clamp(popularity + amount, 0f, 100f);
    }

    // ==========================================
    // 🌟 จบวัน: สรุปยอด และ ล้างระบบบั๊ก
    // ==========================================
    public void EndDay()
    {
        // 1. แก้บั๊ก State ค้าง: สั่ง Hard Reset ระบบทอนเงินทันที
        FareSystem fare = Object.FindFirstObjectByType<FareSystem>();
        if (fare != null)
        {
            fare.ForceResetSystem();
        }

        // 2. หยุดเวลาและจัดการ Cursor
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 3. คำนวณเงิน
        int netProfit = dailyIncome - dailyGasCost;
        totalMoney += netProfit;

        // 4. อัปเดตข้อความ UI
        if (txtDay) txtDay.text = $"สรุปผลวันที่ {currentDay}";
        if (txtIncome) txtIncome.text = $"+ {dailyIncome} ฿";
        if (txtExpense) txtExpense.text = $"- {dailyGasCost} ฿ (ค่าน้ำมัน)";
        if (txtProfit) txtProfit.text = $"กำไรสุทธิ: {netProfit} ฿\nเงินรวมทั้งหมด: {totalMoney} ฿";
        if (txtPopularity) txtPopularity.text = $"ความนิยม: {popularity:F0} %";

        if (summaryPanel != null) summaryPanel.SetActive(true);
    }

    // ==========================================
    // เริ่มวันใหม่: ล้างคนเก่า ล้างบั๊ก
    // ==========================================
    public void StartNextDay()
    {
        currentDay++;
        stopsReached = 0;
        dailyIncome = 0;

        // 1. สั่งล้างระบบทอนเงินอีกรอบเพื่อความชัวร์ (Double Check)
        FareSystem fare = Object.FindFirstObjectByType<FareSystem>();
        if (fare != null) fare.ForceResetSystem();

        // 2. ลบ NPC ที่ค้างอยู่ในรถให้หมด
        PassengerAI[] remainingPassengers = Object.FindObjectsByType<PassengerAI>(FindObjectsSortMode.None);
        foreach (PassengerAI p in remainingPassengers)
        {
            Destroy(p.gameObject);
        }

        // 3. ปิดหน้าจอสรุปผลและรันเกมต่อ
        if (summaryPanel != null) summaryPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("เริ่มวันใหม่! ลุยเก็บเงินสร้างตัว!");
    }
}