using UnityEngine;

public class NPCMoneyItem : MonoBehaviour
{
    public int moneyValue = 10;
    private bool isCollected = false;
    private FareSystem fareSystem;

    void Start()
    {
        // แบบใหม่: ใช้ FindFirstObjectByType แทน FindObjectOfType
        fareSystem = FindFirstObjectByType<FareSystem>();
    }

    void OnMouseDown()
    {
        if (isCollected) return;

        if (fareSystem != null)
        {
            fareSystem.ReceiveNPCMoney(moneyValue);
        }

        isCollected = true;
        Destroy(gameObject);
    }
}
