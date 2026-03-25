using UnityEngine;

public class BusSeat : MonoBehaviour
{
    [Header("สถานะเบาะ")]
    public bool isBroken = true;
    public int repairCost = 150;

    [Header("โมเดลเบาะ 3D")]
    public GameObject brokenSeatModel;
    public GameObject goodSeatModel;

    [Header("UI (ถ้ามี)")]
    public GameObject repairPromptUI;

    void Start()
    {
        UpdateSeatVisual();
    }

    public void InteractToRepair()
    {
        if (!isBroken) return;

        // ✅ แก้: ใช้ PlayerWallet แทน GameManager.totalMoney
        if (PlayerWallet.Instance == null)
        {
            Debug.LogWarning("ไม่พบ PlayerWallet ในฉาก!");
            return;
        }

        if (PlayerWallet.Instance.GetMoney() >= repairCost)
        {
            PlayerWallet.Instance.AddMoney(-repairCost);
            isBroken = false;
            UpdateSeatVisual();

            Debug.Log($"<color=green>ซ่อมเบาะสำเร็จ! เสียเงิน {repairCost} บาท</color>");
        }
        else
        {
            Debug.Log("<color=red>เงินไม่พอซ่อมเบาะ ไปเก็บค่าตั๋วมาก่อน!</color>");
        }
    }

    public void UpdateSeatVisual()
    {
        if (brokenSeatModel != null) brokenSeatModel.SetActive(isBroken);
        if (goodSeatModel != null)   goodSeatModel.SetActive(!isBroken);
        if (repairPromptUI != null)  repairPromptUI.SetActive(isBroken);
    }
}