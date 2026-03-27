using UnityEngine;

public class BusSeat : MonoBehaviour
{
    public enum SeatState { Broken, Empty, Usable }

    [Header("สถานะปัจจุบัน")]
    public SeatState currentState = SeatState.Broken;

    [Header("ราคา")]
    public int sellPrice = 20;   // ราคาขายเศษเหล็กเก้าอี้พัง

    [Header("โมเดลในแต่ละสถานะ")]
    public GameObject brokenModel;
    public GameObject emptyModel; // อาจจะเป็นแค่รอยน็อตบนพื้น หรือไม่ต้องใส่ก็ได้
    public GameObject goodModel;

    void Start()
    {
        UpdateVisuals();
    }

    // ฟังก์ชันสำหรับ "ขายทิ้ง" (เรียกใช้ตอนผู้เล่นคลิกที่เก้าอี้พัง)
    public void InteractToSell()
    {
        if (currentState == SeatState.Broken)
        {
            PlayerWallet.Instance.AddMoney(sellPrice); // ได้เงินค่าเศษเหล็ก
            currentState = SeatState.Empty;            // เปลี่ยนสถานะเป็นที่ว่าง
            UpdateVisuals();
            Debug.Log($"ขายเก้าอี้พังได้เงิน {sellPrice} บาท! ตอนนี้เป็นพื้นที่ว่างแล้ว");
            // 🌟 ทริค: ใส่เสียงรื้อถอน หรือเสียงเก็บเหรียญ
        }
    }

    // ฟังก์ชันสำหรับ "ติดตั้งเบาะใหม่" (เรียกใช้จากระบบร้านค้าในมือถือ)
    public void InstallNewSeat()
    {
        if (currentState == SeatState.Empty)
        {
            currentState = SeatState.Usable;
            UpdateVisuals();
            Debug.Log("ติดตั้งเก้าอี้ใหม่เรียบร้อย!");
            // 🌟 ทริค: ใส่เสียงวิ้งๆ หรือเสียงประกอบร่าง
        }
    }

    public void UpdateVisuals()
    {
        if (brokenModel != null) brokenModel.SetActive(currentState == SeatState.Broken);
        if (emptyModel != null) emptyModel.SetActive(currentState == SeatState.Empty);
        if (goodModel != null) goodModel.SetActive(currentState == SeatState.Usable);
    }
}