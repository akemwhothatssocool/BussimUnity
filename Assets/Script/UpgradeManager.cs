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
    public int gasReductionAmount = 50; 
    public TextMeshProUGUI txtEngineCost;

    [Header("=== 2. อัปเกรดเบาะนั่ง (เพิ่มความนิยม) ===")]
    public int seatUpgradeCost = 600;
    public float popularityBoost = 15f; 
    public TextMeshProUGUI txtSeatCost;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    public void OpenMenu()
    {
        if (upgradePanel != null) upgradePanel.SetActive(true);
        UpdateUI();
    }

    public void CloseMenu()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    public void UpdateUI()
    {
        // 🌟 ดึงเงินจาก PlayerWallet มาโชว์
        if (txtTotalMoney && PlayerWallet.Instance != null) 
        {
            // 💡 หมายเหตุ: ถ้าใน PlayerWallet ของคุณไม่ได้ตั้งชื่อตัวแปรว่า currentMoney ให้แก้ชื่อตรงนี้ให้ตรงกันนะครับ (เช่น อาจจะชื่อ money เฉยๆ)
            txtTotalMoney.text = $"เงินเก็บ: {PlayerWallet.Instance.currentMoney} ฿"; 
        }

        if (txtEngineCost) txtEngineCost.text = $"{engineUpgradeCost} ฿";
        if (txtSeatCost) txtSeatCost.text = $"{seatUpgradeCost} ฿";
    }

    // ==========================================
    // ปุ่มกดซื้ออัปเกรด
    // ==========================================
    public void BuyEngineUpgrade()
    {
        // 🌟 เช็กเงินจาก PlayerWallet
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.currentMoney >= engineUpgradeCost)
        {
            // 1. หักเงิน โดยใช้คำสั่ง AddMoney แต่ส่งค่าติดลบเข้าไป
            PlayerWallet.Instance.AddMoney(-engineUpgradeCost);
            
            // 2. ลดค่าน้ำมัน 
            if (GameManager.Instance != null)
                GameManager.Instance.dailyGasCost = Mathf.Max(0, GameManager.Instance.dailyGasCost - gasReductionAmount);
            
            // 3. เพิ่มราคาอัปเกรดครั้งต่อไป
            engineUpgradeCost += 300; 
            
            UpdateUI();
            Debug.Log("อัปเกรดเครื่องยนต์สำเร็จ! ค่าน้ำมันถูกลงแล้ว");
        }
        else
        {
            Debug.Log("เงินไม่พอซื้อเครื่องยนต์จ้า!");
        }
    }

    public void BuySeatUpgrade()
    {
        // 🌟 เช็กเงินจาก PlayerWallet
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.currentMoney >= seatUpgradeCost)
        {
            // 1. หักเงิน
            PlayerWallet.Instance.AddMoney(-seatUpgradeCost);
            
            // 2. เพิ่มโบนัสความนิยมถาวรใน GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.permanentPopularityBonus += popularityBoost;
            
            // 3. เพิ่มราคาอัปเกรดครั้งต่อไป
            seatUpgradeCost += 400; 
            
            // อัปเดตดาวโชว์ทันที
            if (GameManager.Instance != null && GameManager.Instance.busRateDisplay != null)
            {
                float finalPop = Mathf.Clamp(GameManager.Instance.popularity + GameManager.Instance.permanentPopularityBonus, 0f, 100f);
                GameManager.Instance.busRateDisplay.UpdateBusRate(finalPop / 20f);
            }

            UpdateUI();
            Debug.Log($"อัปเกรดเบาะสำเร็จ! ได้โบนัสถาวร +{popularityBoost}%");
        }
        else
        {
            Debug.Log("เงินไม่พอซื้อเบาะจ้า!");
        }
    }
}