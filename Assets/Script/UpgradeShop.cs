using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class UpgradeShop : MonoBehaviour
{
    [Header("ตั้งค่าร้านค้าอู่รถเมล์")]
    public int newSeatPrice = 150;

    [Header("UI แจ้งเตือน (ลาก Text มาใส่)")]
    public TextMeshProUGUI txtShopFeedback; // เอาไว้บอกว่าซื้อสำเร็จ หรือเงินไม่พอ

    // ฟังก์ชันนี้จะถูกเรียกตอนผู้เล่นกดปุ่ม "ซื้อเบาะ" ในหน้าจบวัน
    public void BuyNewSeat()
    {
        // 1. เช็คเงินในกระเป๋า
        if (PlayerWallet.Instance.GetMoney() < newSeatPrice)
        {
            if (txtShopFeedback != null)
                txtShopFeedback.text = "<color=red>เงินไม่พอเฮีย! ไปวิ่งรถหาเงินมาก่อน</color>";
            return;
        }

        // 2. หาเบาะที่ถูกรื้อทิ้งไปแล้ว (สถานะ Empty)
        BusSeat[] allSeats = Object.FindObjectsByType<BusSeat>(FindObjectsSortMode.None);
        List<BusSeat> emptySeats = allSeats.Where(s => s.currentState == BusSeat.SeatState.Empty).ToList();

        // 3. ทำการสั่งซื้อและติดตั้ง
        if (emptySeats.Count > 0)
        {
            PlayerWallet.Instance.AddMoney(-newSeatPrice); // หักเงิน
            emptySeats[0].InstallNewSeat(); // ติดตั้งเบาะใหม่

            if (txtShopFeedback != null)
                txtShopFeedback.text = $"<color=green>ติดตั้งเบาะใหม่เรียบร้อย! (เหลือเงิน {PlayerWallet.Instance.GetMoney()} ฿)</color>";
        }
        else
        {
            if (txtShopFeedback != null)
                txtShopFeedback.text = "<color=yellow>ซื้อไม่ได้! รถเต็มแล้ว หรือยังไม่ได้รื้อซากเบาะพังทิ้ง!</color>";
        }
    }
}