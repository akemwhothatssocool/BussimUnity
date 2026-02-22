using UnityEngine;

public class Money3DInteract : MonoBehaviour
{
    [Header("ค่าเงินของชิ้นนี้")]
    public int moneyValue = 20;

    private FareSystem fareSystem;

    void Start()
    {
        // หาตัว FareSystem ในฉากอัตโนมัติ
        fareSystem = FindObjectOfType<FareSystem>();
    }

    void OnMouseDown()
    {
        if (fareSystem != null)
        {
            // ส่งค่าเงินไปให้ระบบคิดเงิน
            fareSystem.AddChange(moneyValue);
        }
    }
}