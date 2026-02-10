using UnityEngine;

public class TestBusSystem : MonoBehaviour
{
    // เปลี่ยนจาก FareSystem เป็น BusStopManager
    public BusStopManager busStopManager;

    // เอาฟังก์ชันนี้ไปผูกกับปุ่ม "เรียกลูกค้า" เหมือนเดิม
    public void CallCustomer()
    {
        if (busStopManager != null)
        {
            // สั่งให้เสกคนออกมา (เดี๋ยวคนจะเดินไปนั่ง แล้วให้เรากด E เอง)
            busStopManager.SpawnPassenger();
        }
        else
        {
            Debug.LogError("ลืมลาก BusStopManager มาใส่ในช่องครับ!");
        }
    }
}