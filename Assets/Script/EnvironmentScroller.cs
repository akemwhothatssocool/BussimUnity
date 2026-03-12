using UnityEngine;

public class EnvironmentScroller : MonoBehaviour
{
    [Header("⚙️ ตั้งค่าความเร็วทิวทัศน์")]
    [Tooltip("ใส่ค่าบวก = วิ่งไปขวา | ใส่ค่าลบ = วิ่งไปซ้าย")]
    public float scrollSpeed = 10f; // ตั้งตึกกับถนนให้เลขเท่ากันเป๊ะๆ

    [Header("🔄 ระบบวนลูปฉาก (แกน X)")]
    public float xStartPosition = -100f;  // จุดเกิด (คิวหน้าสุด)
    public float xResetPosition = 100f;   // จุดหาย (คิวท้ายสุด)

    void Update()
    {
        // 1. เลื่อนฉากตลอดเวลา
        transform.Translate(Vector3.right * scrollSpeed * Time.deltaTime, Space.World);

        // 2. ระบบเช็ควาร์ป (แบบชดเชยรอยต่อ ไม่ให้ฉากขาด)
        if (scrollSpeed > 0) // กรณีวิ่งไปทาง "ขวา"
        {
            if (transform.position.x >= xResetPosition)
            {
                // คำนวณระยะที่วิ่งทะลุเกินไป เพื่อไม่ให้มีช่องโหว่ระหว่างแผ่น
                float overshoot = transform.position.x - xResetPosition; 
                transform.position = new Vector3(xStartPosition + overshoot, transform.position.y, transform.position.z);
            }
        }
        else if (scrollSpeed < 0) // กรณีวิ่งไปทาง "ซ้าย"
        {
            if (transform.position.x <= xResetPosition)
            {
                float overshoot = transform.position.x - xResetPosition;
                transform.position = new Vector3(xStartPosition + overshoot, transform.position.y, transform.position.z);
            }
        }
    }
}