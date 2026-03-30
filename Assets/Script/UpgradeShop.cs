using TMPro;
using UnityEngine;

public class UpgradeShop : MonoBehaviour
{
    [Header("Seat Prices")]
    public int priceLv1 = 150;
    public int priceLv2 = 450;
    public int priceLv3 = 900;

    [Header("Feedback UI")]
    public TextMeshProUGUI txtShopFeedback;

    public void BuySeat(int level)
    {
        SeatDeliveryManager deliveryManager = SeatDeliveryManager.GetOrCreateInstance();
        if (deliveryManager == null)
        {
            SetFeedback("<color=red>ระบบส่งเก้าอี้ยังไม่พร้อมใช้งาน</color>");
            return;
        }

        int cost = GetPriceForLevel(level);
        if (deliveryManager.TryOrderSeatDelivery(level, cost, out string feedback))
        {
            SetFeedback(feedback);

            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.CloseMenu();

            return;
        }

        SetFeedback(feedback);
    }

    public void BuyNewSeat()
    {
        BuySeat(1);
    }

    int GetPriceForLevel(int level)
    {
        level = Mathf.Clamp(level, 1, BusSeat.MaxSupportedSeatLevel);
        return level switch
        {
            1 => priceLv1,
            2 => priceLv2,
            3 => priceLv3,
            _ => 0
        };
    }

    void SetFeedback(string message)
    {
        if (txtShopFeedback != null)
            txtShopFeedback.text = message;
    }
}
