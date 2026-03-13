using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance; // ✅ Singleton เรียกได้จากทุกที่

    [Header("ตั้งค่าเงินเริ่มต้น")]
    public int startingMoney = 100;

    [Header("UI แสดงเงินตอนเดินปกติ")]
    public TextMeshProUGUI textWalletHUD; // ✅ ลาก Text ใน HUD มาใส่

    private int currentMoney;

    void Awake()
    {
        // ✅ Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentMoney = startingMoney;
        UpdateHUD();
    }

    // ✅ รับเงิน (ผู้เล่นเก็บค่าโดยสารได้)
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateHUD();
        Debug.Log($"รับเงิน +{amount} ฿ | รวม: {currentMoney} ฿");
    }

    // ✅ จ่ายเงิน (ทอนเงินให้ผู้โดยสาร)
    public bool SpendMoney(int amount)
    {
        if (currentMoney < amount)
        {
            Debug.LogWarning("เงินไม่พอ!");
            return false;
        }
        currentMoney -= amount;
        UpdateHUD();
        Debug.Log($"จ่ายเงิน -{amount} ฿ | รวม: {currentMoney} ฿");
        return true;
    }

    public int GetMoney() => currentMoney;

    void UpdateHUD()
    {
        if (textWalletHUD != null)
            textWalletHUD.text = currentMoney.ToString() + " ฿";
    }
}