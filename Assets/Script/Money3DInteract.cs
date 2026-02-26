using UnityEngine;

public class Money3DInteract : MonoBehaviour
{
    [Header("ค่าเงินของชิ้นนี้")]
    public int moneyValue = 20;

    [Header("✨ โมเดล 3D ที่ต้องการให้ขยับ")]
    public Transform visualModel; // 🌟 ลาก "ตัวลูก" ที่เป็นโมเดลภาพ 3D มาใส่ช่องนี้

    [Header("✨ เอฟเฟกต์ตอนเมาส์ชี้ (Hover)")]
    public float hoverOffsetX = 0.05f;
    public float hoverOffsetY = 0f; // ✅ เพิ่มแกน Y 
    public float hoverOffsetZ = 0f; // ✅ เพิ่มแกน Z ให้ด้วยเผื่อได้ใช้
    public float hoverSpeed = 15f;

    private FareSystem fareSystem;
    private Vector3 originalLocalPos;
    private Vector3 targetLocalPos;
    private bool isHovered = false;

    void Start()
    {
        fareSystem = FindFirstObjectByType<FareSystem>();

        // จำตำแหน่งเดิมของ "ตัวลูก" เอาไว้
        if (visualModel != null)
        {
            originalLocalPos = visualModel.localPosition;
        }
        else
        {
            Debug.LogWarning("⚠️ อย่าลืมลากโมเดลตัวลูก มาใส่ในช่อง Visual Model นะครับ!");
        }
    }

    void Update()
    {
        isHovered = false;

        if (Cursor.visible)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // ถ้ายิงเลเซอร์ชนตัวพ่อ (ซึ่งตัวพ่ออยู่นิ่งๆ ตลอดเวลา)
                if (hit.collider.gameObject == gameObject)
                {
                    isHovered = true;

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (fareSystem != null) fareSystem.AddChange(moneyValue);
                    }
                }
            }
        }

        // 🎯 สั่งขยับ "ตัวลูก" แทน 
        if (visualModel != null)
        {
            if (isHovered)
            {
                // ✅ เอาค่าแกน X, Y, Z มาบวกเพิ่มตรงนี้เลย
                targetLocalPos = originalLocalPos + new Vector3(hoverOffsetX, hoverOffsetY, hoverOffsetZ);
            }
            else
            {
                targetLocalPos = originalLocalPos;
            }

            // ตัวลูกขยับ แต่ตัวพ่อ(Collider) ยังอยู่ที่เดิม!
            visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, targetLocalPos, Time.deltaTime * hoverSpeed);
        }
    }
}