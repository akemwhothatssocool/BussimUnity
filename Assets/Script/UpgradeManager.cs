using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("=== หน้าต่าง UI ===")]
    public GameObject upgradePanel;
    public TextMeshProUGUI txtTotalMoney; // โชว์เงินมุมขวาบน

    [Header("=== 1. อัปเกรดเครื่องยนต์ (Engine - เพิ่มความเร็ว) ===")]
    public int engineUpgradeCost = 300;
    public TextMeshProUGUI txtEngineCost;

    [Header("=== 2. อัปเกรดถังน้ำมัน (Fuel - ลดค่าน้ำมันรายวัน) ===")]
    public int fuelUpgradeCost = 300;
    public int gasReductionAmount = 50;
    public TextMeshProUGUI txtFuelCost;

    [Header("=== 3. อัปเกรดเบาะนั่ง (Seat - เพิ่มความนิยม) ===")]
    public int seatUpgradeCost = 300;
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
        // อัปเดตเงินผู้เล่น
        if (txtTotalMoney && PlayerWallet.Instance != null)
        {
            txtTotalMoney.text = $"{PlayerWallet.Instance.currentMoney}";
        }

        // อัปเดตราคาบนปุ่ม
        if (txtEngineCost) txtEngineCost.text = $"{engineUpgradeCost}";
        if (txtFuelCost) txtFuelCost.text = $"{fuelUpgradeCost}";
        if (txtSeatCost) txtSeatCost.text = $"{seatUpgradeCost}";
    }

    // ==========================================
    // 1. ปุ่มซื้อเครื่องยนต์ (เพิ่มความเร็ว)
    // ==========================================
    public void BuyEngineUpgrade()
    {
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.currentMoney >= engineUpgradeCost)
        {
            PlayerWallet.Instance.AddMoney(-engineUpgradeCost);

            // 🌟 TODO: อนาคตเราค่อยเอาตัวแปรความเร็วจาก CityManager มาบวกเพิ่มตรงนี้ครับ
            // เช่น CityManager.Instance.busSpeed += 2f;
            Debug.Log("อัปเกรดเครื่องยนต์สำเร็จ! (รอเขียนระบบเพิ่มความเร็ว)");

            engineUpgradeCost += 500;
            UpdateUI();
        }
    }

    // ==========================================
    // 2. ปุ่มซื้อถังน้ำมัน (ลดค่าน้ำมัน)
    // ==========================================
    public void BuyFuelUpgrade()
    {
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.currentMoney >= fuelUpgradeCost)
        {
            PlayerWallet.Instance.AddMoney(-fuelUpgradeCost);

            // ลดค่าน้ำมันรายวันใน GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.dailyGasCost = Mathf.Max(0, GameManager.Instance.dailyGasCost - gasReductionAmount);

            fuelUpgradeCost += 500;
            UpdateUI();
            Debug.Log("อัปเกรดถังน้ำมันสำเร็จ! ค่าน้ำมันถูกลงแล้ว");
        }
    }

    // ==========================================
    // 3. ปุ่มซื้อเบาะนั่ง (เพิ่มโบนัสดาว)
    // ==========================================
    public void BuySeatUpgrade()
    {
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.currentMoney >= seatUpgradeCost)
        {
            PlayerWallet.Instance.AddMoney(-seatUpgradeCost);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.permanentPopularityBonus += popularityBoost;

                // อัปเดตดาวโชว์ทันที
                if (GameManager.Instance.busRateDisplay != null)
                {
                    float finalPop = Mathf.Clamp(GameManager.Instance.popularity + GameManager.Instance.permanentPopularityBonus, 0f, 100f);
                    GameManager.Instance.busRateDisplay.UpdateBusRate(finalPop / 20f);
                }
            }

            seatUpgradeCost += 500;
            UpdateUI();
            Debug.Log($"อัปเกรดเบาะสำเร็จ! ได้โบนัสถาวร +{popularityBoost}%");
        }
    }
}