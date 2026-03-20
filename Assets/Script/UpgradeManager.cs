using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("=== หน้าต่าง UI ===")]
    public GameObject upgradePanel;
    public TextMeshProUGUI txtTotalMoney; // โชว์เงินที่เรามีตอนนี้

    [Header("=== 1. อัปเกรดเครื่องยนต์ (ลดค่าน้ำมัน) ===")]
    public int engineUpgradeCost = 500;
    public int gasReductionAmount = 50; // ลดค่าน้ำมันลงทีละ 50 บาท
    public TextMeshProUGUI txtEngineCost;

    [Header("=== 2. อัปเกรดเบาะนั่ง (เพิ่มความนิยม) ===")]
    public int seatUpgradeCost = 600;
    public float popularityBoost = 15f; // เพิ่มความนิยม 15% ทันที
    public TextMeshProUGUI txtSeatCost;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    // ฟังก์ชันเปิดหน้าจออัปเกรด (เรียกจากปุ่ม Upgrade Bus ในหน้าจบวัน)
    public void OpenMenu()
    {
        if (upgradePanel != null) upgradePanel.SetActive(true);
        UpdateUI();
    }

    // ฟังก์ชันปิดหน้าจออัปเกรด
    public void CloseMenu()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    // อัปเดตตัวเลขบนหน้าจอ
    public void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        if (txtTotalMoney) txtTotalMoney.text = $"เงินเก็บ: {GameManager.Instance.totalMoney} ฿";
        if (txtEngineCost) txtEngineCost.text = $"{engineUpgradeCost} ฿";
        if (txtSeatCost) txtSeatCost.text = $"{seatUpgradeCost} ฿";
    }

    // ==========================================
    // ปุ่มกดซื้ออัปเกรด
    // ==========================================
    public void BuyEngineUpgrade()
    {
        if (GameManager.Instance.totalMoney >= engineUpgradeCost)
        {
            // 1. หักเงิน
            GameManager.Instance.totalMoney -= engineUpgradeCost;

            // 2. ลดค่าน้ำมัน (ห้ามติดลบ)
            GameManager.Instance.dailyGasCost = Mathf.Max(0, GameManager.Instance.dailyGasCost - gasReductionAmount);

            // 3. เพิ่มราคาอัปเกรดครั้งต่อไป (ให้เกมยากขึ้น)
            engineUpgradeCost += 300;

            UpdateUI();
            Debug.Log("อัปเกรดเครื่องยนต์สำเร็จ! ค่าน้ำมันถูกลงแล้ว");
        }
        else
        {
            Debug.Log("เงินไม่พอซื้อเครื่องยนต์จ้า!");
            // TODO: อนาคตอาจจะทำเสียง "ติ๊ดๆ" หรือข้อความเด้งเตือนว่าเงินไม่พอ
        }
    }

    public void BuySeatUpgrade()
    {
        if (GameManager.Instance.totalMoney >= seatUpgradeCost)
        {
            // 1. หักเงิน
            GameManager.Instance.totalMoney -= seatUpgradeCost;

            // 2. 🌟 ย้ายไปบวกที่ "โบนัสถาวร" แทน! (ผู้โดยสารโกรธก็หักตัวนี้ไม่ได้แล้ว)
            GameManager.Instance.permanentPopularityBonus += popularityBoost;

            // 3. เพิ่มราคาอัปเกรดครั้งต่อไป
            seatUpgradeCost += 400;

            UpdateUI();
            Debug.Log($"อัปเกรดเบาะสำเร็จ! ได้โบนัสถาวร +{popularityBoost}%");
        }
        else
        {
            Debug.Log("เงินไม่พอซื้อเบาะจ้า!");
        }
    }
}