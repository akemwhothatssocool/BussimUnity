using UnityEngine;

public class Money3DInteract : MonoBehaviour
{
    [Header("ค่าเงินของชิ้นนี้")]
    public int moneyValue = 20;

    [Header("✨ เอฟเฟกต์ตอนเมาส์ชี้ (Hover)")]
    public float hoverOffsetX = 0.03f; // ระยะที่จะให้เด้งออกมาในแกน X Local (ปรับค่าได้)
    public float hoverSpeed = 10f;     // ความเร็วในการเด้งและหดกลับ

    private FareSystem fareSystem;
    private Vector3 originalLocalPos;
    private Vector3 targetLocalPos;
    private bool isHovered = false;

    void Start()
    {
        // หาตัว FareSystem ในฉากอัตโนมัติ
        fareSystem = FindFirstObjectByType<FareSystem>();

        // จำตำแหน่งเดิมของมันเอาไว้ตั้งแต่เริ่ม
        originalLocalPos = transform.localPosition;
    }

    void Update()
    {
        // รีเซ็ตสถานะทุกเฟรมก่อน
        isHovered = false;

        // เช็คว่าเมาส์โชว์อยู่ (เปิดหน้าคิดเงิน)
        if (Cursor.visible)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // ถ้าเลเซอร์ยิงโดนตัวเอง แปลว่าเมาส์กำลังชี้อยู่!
                if (hit.collider.gameObject == gameObject)
                {
                    isHovered = true;

                    // ถ้าชี้อยู่แล้วกดคลิกซ้ายด้วย
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (fareSystem != null)
                        {
                            fareSystem.AddChange(moneyValue);
                        }
                    }
                }
            }
        }

        // 🎯 สั่งการขยับ: ถ้าชี้อยู่ ให้บวกแกน X เพิ่มเข้าไป ถ้าไม่ชี้ ให้กลับเป้าหมายเดิม
        if (isHovered)
        {
            targetLocalPos = originalLocalPos + new Vector3(hoverOffsetX, 0f, 0f);
        }
        else
        {
            targetLocalPos = originalLocalPos;
        }

        // เคลื่อนที่อย่างสมูท (Lerp) จากจุดปัจจุบัน ไปยังจุดเป้าหมาย
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * hoverSpeed);
    }
}