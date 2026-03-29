using UnityEngine;

public class BusSeat : MonoBehaviour
{
    public enum SeatState { Broken, Empty, Usable }
    public enum SeatLevel { None = 0, Lv1 = 1, Lv2 = 2, Lv3 = 3 }

    [Header("สถานะปัจจุบัน")]
    public SeatState currentState = SeatState.Broken;
    public SeatLevel currentLevel = SeatLevel.None;

    [Header("ราคา")]
    public int sellPrice = 20;   // ราคาขายเศษเหล็กเก้าอี้พัง

    [Header("โมเดลในแต่ละสถานะ")]
    public GameObject brokenModel;
    public GameObject emptyModel; // อาจจะเป็นแค่รอยน็อตบนพื้น หรือไม่ต้องใส่ก็ได้
    public GameObject goodModel;
    public GameObject goodModelLv1;
    public GameObject goodModelLv2;
    public GameObject goodModelLv3;

    [Header("Save")]
    [SerializeField] private string seatId;

    void Start()
    {
        EnsureSeatId();
        NormalizeState();
        UpdateVisuals();
    }

    // ฟังก์ชันสำหรับ "ขายทิ้ง" (เรียกใช้ตอนผู้เล่นคลิกที่เก้าอี้พัง)
    public void InteractToSell()
    {
        if (currentState == SeatState.Broken)
        {
            PlayerWallet.Instance.AddMoney(sellPrice); // ได้เงินค่าเศษเหล็ก
            currentState = SeatState.Empty;            // เปลี่ยนสถานะเป็นที่ว่าง
            currentLevel = SeatLevel.None;
            UpdateVisuals();
            SaveSystem.SaveCurrentGame();
            Debug.Log($"ขายเก้าอี้พังได้เงิน {sellPrice} บาท! ตอนนี้เป็นพื้นที่ว่างแล้ว");
            // 🌟 ทริค: ใส่เสียงรื้อถอน หรือเสียงเก็บเหรียญ
        }
    }

    // ฟังก์ชันสำหรับ "ติดตั้งเบาะใหม่" (เรียกใช้จากระบบร้านค้าในมือถือ)
    public void InstallNewSeat(int level = 1)
    {
        if (currentState == SeatState.Empty)
        {
            currentState = SeatState.Usable;
            currentLevel = ClampLevel(level);
            UpdateVisuals();
            SaveSystem.SaveCurrentGame();
            Debug.Log("ติดตั้งเก้าอี้ใหม่เรียบร้อย!");
            // 🌟 ทริค: ใส่เสียงวิ้งๆ หรือเสียงประกอบร่าง
        }
    }

    public bool IsUsableForPassengers()
    {
        return currentState == SeatState.Usable;
    }

    public int GetTipBonus()
    {
        return currentLevel switch
        {
            SeatLevel.Lv2 => 2,
            SeatLevel.Lv3 => 5,
            _ => 0
        };
    }

    public float GetPatienceDecayMultiplier()
    {
        return currentLevel == SeatLevel.Lv3 ? 0.5f : 1f;
    }

    public string GetSeatId()
    {
        EnsureSeatId();
        return seatId;
    }

    public SeatSaveData CaptureSaveData()
    {
        return new SeatSaveData
        {
            seatId = GetSeatId(),
            state = (int)currentState,
            level = (int)currentLevel
        };
    }

    public void ApplySaveData(SeatSaveData data)
    {
        if (data == null) return;

        currentState = (SeatState)data.state;
        currentLevel = (SeatLevel)data.level;
        NormalizeState();
        UpdateVisuals();
    }

    public static void ApplySavedSeats(SeatSaveData[] seatStates)
    {
        if (seatStates == null || seatStates.Length == 0) return;

        BusSeat[] seats = Object.FindObjectsByType<BusSeat>(FindObjectsSortMode.None);
        foreach (BusSeat seat in seats)
        {
            if (seat == null) continue;

            seat.EnsureSeatId();
            for (int i = 0; i < seatStates.Length; i++)
            {
                if (seatStates[i] == null) continue;
                if (seatStates[i].seatId != seat.seatId) continue;

                seat.ApplySaveData(seatStates[i]);
                break;
            }
        }
    }

    public static BusSeat ResolveSeatForPoint(Transform seatPoint)
    {
        if (seatPoint == null) return null;

        BusSeat directSeat = seatPoint.GetComponent<BusSeat>();
        if (directSeat != null) return directSeat;

        BusSeat parentSeat = seatPoint.GetComponentInParent<BusSeat>();
        if (parentSeat != null) return parentSeat;

        BusSeat childSeat = seatPoint.GetComponentInChildren<BusSeat>(true);
        if (childSeat != null) return childSeat;

        BusSeat[] allSeats = Object.FindObjectsByType<BusSeat>(FindObjectsSortMode.None);
        BusSeat closestSeat = null;
        float closestDistance = 0.6f;

        foreach (BusSeat seat in allSeats)
        {
            if (seat == null) continue;

            float distance = Vector3.Distance(seat.transform.position, seatPoint.position);
            if (distance > closestDistance) continue;

            closestDistance = distance;
            closestSeat = seat;
        }

        return closestSeat;
    }

    public void UpdateVisuals()
    {
        if (brokenModel != null) brokenModel.SetActive(currentState == SeatState.Broken);
        if (emptyModel != null) emptyModel.SetActive(currentState == SeatState.Empty);
        bool usingLevelModels = goodModelLv1 != null || goodModelLv2 != null || goodModelLv3 != null;

        if (goodModel != null)
        {
            bool useFallbackGoodModel = currentState == SeatState.Usable &&
                (!usingLevelModels || currentLevel == SeatLevel.Lv1 && goodModelLv1 == null);
            goodModel.SetActive(useFallbackGoodModel);
        }

        if (goodModelLv1 != null) goodModelLv1.SetActive(currentState == SeatState.Usable && currentLevel == SeatLevel.Lv1);
        if (goodModelLv2 != null) goodModelLv2.SetActive(currentState == SeatState.Usable && currentLevel == SeatLevel.Lv2);
        if (goodModelLv3 != null) goodModelLv3.SetActive(currentState == SeatState.Usable && currentLevel == SeatLevel.Lv3);
    }

    void EnsureSeatId()
    {
        if (!string.IsNullOrEmpty(seatId)) return;
        seatId = BuildHierarchyPath(transform);
    }

    void NormalizeState()
    {
        if (currentState == SeatState.Usable && currentLevel == SeatLevel.None)
            currentLevel = SeatLevel.Lv1;

        if (currentState != SeatState.Usable)
            currentLevel = SeatLevel.None;
    }

    SeatLevel ClampLevel(int level)
    {
        if (level <= 1) return SeatLevel.Lv1;
        if (level == 2) return SeatLevel.Lv2;
        return SeatLevel.Lv3;
    }

    static string BuildHierarchyPath(Transform target)
    {
        if (target == null) return string.Empty;

        string path = $"{target.name}[{target.GetSiblingIndex()}]";
        Transform current = target.parent;

        while (current != null)
        {
            path = $"{current.name}[{current.GetSiblingIndex()}]/{path}";
            current = current.parent;
        }

        return path;
    }
}
