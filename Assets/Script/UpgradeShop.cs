using System.Collections.Generic;
using System.Linq;
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
        int cost = GetPriceForLevel(level);
        if (cost <= 0)
        {
            SetFeedback("<color=red>ระดับเก้าอี้ไม่ถูกต้อง</color>");
            return;
        }

        if (PlayerWallet.Instance == null)
        {
            SetFeedback("<color=red>ไม่พบกระเป๋าเงินผู้เล่น</color>");
            return;
        }

        int currentMoney = PlayerWallet.Instance.GetMoney();
        if (currentMoney < cost)
        {
            SetFeedback($"<color=red>เงินไม่พอ! ขาดอีก {cost - currentMoney} ฿</color>");
            return;
        }

        BusSeat[] allSeats = Object.FindObjectsByType<BusSeat>(FindObjectsSortMode.None);
        List<BusSeat> emptySeats = allSeats
            .Where(seat => seat != null && seat.currentState == BusSeat.SeatState.Empty)
            .OrderBy(seat => seat.GetSeatId())
            .ToList();

        if (emptySeats.Count == 0)
        {
            SetFeedback("<color=yellow>รถเต็มแล้ว! ต้องรื้อเบาะเก่าทิ้งก่อนถึงจะซื้อใหม่ได้</color>");
            return;
        }

        PlayerWallet.Instance.AddMoney(-cost);
        emptySeats[0].InstallNewSeat(level);

        string bonusText = level switch
        {
            2 => " ได้ทิปเพิ่ม +2 บาทต่อคน",
            3 => " ได้ทิปเพิ่ม +5 บาท และผู้โดยสารใจเย็นขึ้น",
            _ => string.Empty
        };

        SetFeedback($"<color=green>ติดตั้งเก้าอี้ Lv.{level} สำเร็จ!{bonusText}</color>");
    }

    public void BuyNewSeat()
    {
        BuySeat(1);
    }

    int GetPriceForLevel(int level)
    {
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
