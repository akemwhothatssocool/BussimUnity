using UnityEngine;
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
    public int dailyGasCost = 150; // สมมติค่าน้ำมันวันละ 150 บาท
    public int totalMoney = 0;

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
        // ทำให้เรียกใช้ GameManager.Instance ได้จากทุกที่
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);
    }

    // ฟังก์ชันนี้นับป้าย จะถูกเรียกตอนรถเมล์เข้าจอด
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

    // เก็บรายรับรายวัน
    public void AddDailyIncome(int amount)
    {
        dailyIncome += amount;
    }

    // ปรับความนิยม (พวกลบ/บวก ตอนทอนเงิน)
    public void AdjustPopularity(float amount)
    {
        popularity = Mathf.Clamp(popularity + amount, 0f, 100f);
    }

    // จบวัน: สรุปยอด
    public void EndDay()
    {
        Time.timeScale = 0f; // หยุดเวลาในเกมชั่วคราว
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        int netProfit = dailyIncome - dailyGasCost;
        totalMoney += netProfit; // เอาเข้ากระเป๋าจริง

        // อัปเดตข้อความ UI
        if (txtDay) txtDay.text = $"สรุปผลวันที่ {currentDay}";
        if (txtIncome) txtIncome.text = $"+ {dailyIncome} ฿";
        if (txtExpense) txtExpense.text = $"- {dailyGasCost} ฿ (ค่าน้ำมัน)";
        if (txtProfit) txtProfit.text = $"กำไรสุทธิ: {netProfit} ฿";
        if (txtPopularity) txtPopularity.text = $"ความนิยม: {popularity:F0} %";

        if (summaryPanel != null) summaryPanel.SetActive(true);
    }

    // กดปุ่มเพื่อเริ่มวันใหม่
    public void StartNextDay()
    {
        currentDay++;
        stopsReached = 0;
        dailyIncome = 0;

        // ✅ เคลียร์ NPC ที่ยังนั่งอยู่บนรถออกให้หมดเพื่อเริ่มกะใหม่
        PassengerAI[] remainingPassengers = FindObjectsOfType<PassengerAI>();
        foreach (PassengerAI p in remainingPassengers)
        {
            Destroy(p.gameObject);
        }

        if (summaryPanel != null) summaryPanel.SetActive(false);

        Time.timeScale = 1f; // เดินเวลาต่อ
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("เริ่มวันใหม่! ลุย!");
    }
}