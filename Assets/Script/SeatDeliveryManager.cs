using System.Linq;
using UnityEngine;

public class SeatDeliveryManager : MonoBehaviour
{
    public static SeatDeliveryManager Instance;

    [Header("Delivery Spawn")]
    public Transform deliveryDropPoint;
    public Vector3 deliveryDropOffset = new Vector3(0f, 2.4f, 1.6f);
    public Vector3 crateSize = new Vector3(0.5f, 0.42f, 0.68f);

    SeatDeliveryCrate activeCrate;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public static SeatDeliveryManager GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        SeatDeliveryManager existing = Object.FindFirstObjectByType<SeatDeliveryManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject managerObject = new GameObject("SeatDeliveryManager");
        Instance = managerObject.AddComponent<SeatDeliveryManager>();
        return Instance;
    }

    public bool TryOrderSeatDelivery(int level, int cost, out string feedback)
    {
        level = Mathf.Clamp(level, 1, BusSeat.MaxSupportedSeatLevel);

        if (cost <= 0)
        {
            feedback = "<color=red>ระดับเก้าอี้ไม่ถูกต้อง</color>";
            return false;
        }

        if (PlayerWallet.Instance == null)
        {
            feedback = "<color=red>ไม่พบกระเป๋าเงินผู้เล่น</color>";
            return false;
        }

        if (!HasEmptySeatSlot())
        {
            feedback = "<color=yellow>ยังไม่มีช่องเก้าอี้ว่างให้ติดตั้ง</color>";
            return false;
        }

        if (HasPendingDelivery())
        {
            feedback = "<color=yellow>มีเก้าอี้ที่สั่งไว้แล้ว ไปหยิบกล่องก่อน</color>";
            return false;
        }

        int currentMoney = PlayerWallet.Instance.GetMoney();
        if (currentMoney < cost)
        {
            feedback = $"<color=red>เงินไม่พอ! ขาดอีก {cost - currentMoney} ฿</color>";
            return false;
        }

        if (!PlayerWallet.Instance.SpendMoney(cost))
        {
            feedback = "<color=red>ตัดเงินไม่สำเร็จ</color>";
            return false;
        }

        SpawnDeliveryCrate(level, GetDropPosition(), GetDropRotation());
        SaveSystem.SaveCurrentGame();

        feedback = $"<color=green>สั่งเก้าอี้ Lv.{level} แล้ว กล่องกำลังลงมาบนรถ รีบไปหยิบมาติดตั้ง</color>";
        return true;
    }

    public bool TryPickUpCrate(SeatDeliveryCrate crate, BusPlayerController player)
    {
        if (crate == null || player == null || player.IsCarryingSeatPackage())
            return false;

        activeCrate = null;
        player.AttachSeatPackage(crate);
        SaveSystem.SaveCurrentGame();
        return true;
    }

    public bool TryInstallCarriedSeat(BusSeat seat, BusPlayerController player)
    {
        if (seat == null || player == null || !player.IsCarryingSeatPackage())
            return false;

        if (seat.currentState != BusSeat.SeatState.Empty)
            return false;

        int seatLevel = Mathf.Clamp(player.GetCarriedSeatLevel(), 1, 3);
        seat.InstallNewSeat(seatLevel);
        player.ClearCarriedSeatPackage();
        SaveSystem.SaveCurrentGame();
        return true;
    }

    public bool HasPendingDelivery()
    {
        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        return activeCrate != null || (player != null && player.IsCarryingSeatPackage());
    }

    public void CaptureSaveData(GameSaveData data)
    {
        if (data == null)
            return;

        data.hasPendingSeatDeliveryBox = activeCrate != null;
        data.pendingSeatDeliveryLevel = activeCrate != null ? activeCrate.seatLevel : 0;
        data.pendingSeatDeliveryPosition = activeCrate != null ? activeCrate.transform.position : Vector3.zero;
        data.pendingSeatDeliveryRotation = activeCrate != null ? activeCrate.transform.eulerAngles : Vector3.zero;

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        data.isCarryingSeatDelivery = player != null && player.IsCarryingSeatPackage();
        data.carriedSeatDeliveryLevel = data.isCarryingSeatDelivery ? player.GetCarriedSeatLevel() : 0;
    }

    public void ApplySaveData(GameSaveData data)
    {
        ClearRuntimeState();
        if (data == null)
            return;

        if (data.hasPendingSeatDeliveryBox && data.pendingSeatDeliveryLevel > 0)
        {
            SpawnDeliveryCrate(
                data.pendingSeatDeliveryLevel,
                data.pendingSeatDeliveryPosition,
                Quaternion.Euler(data.pendingSeatDeliveryRotation));
        }

        if (data.isCarryingSeatDelivery && data.carriedSeatDeliveryLevel > 0)
        {
            SeatDeliveryCrate carriedCrate = SpawnDeliveryCrate(
                data.carriedSeatDeliveryLevel,
                Vector3.zero,
                Quaternion.identity);

            BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
            if (player != null && carriedCrate != null)
                TryPickUpCrate(carriedCrate, player);
        }
    }

    public void NotifyCrateDestroyed(SeatDeliveryCrate crate)
    {
        if (activeCrate == crate)
            activeCrate = null;
    }

    bool HasEmptySeatSlot()
    {
        return Object.FindObjectsByType<BusSeat>(FindObjectsSortMode.None)
            .Any(seat => seat != null && seat.currentState == BusSeat.SeatState.Empty);
    }

    void ClearRuntimeState()
    {
        if (activeCrate != null)
        {
            Destroy(activeCrate.gameObject);
            activeCrate = null;
        }

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        if (player != null && player.IsCarryingSeatPackage())
            player.ClearCarriedSeatPackage();
    }

    SeatDeliveryCrate SpawnDeliveryCrate(int level, Vector3 position, Quaternion rotation)
    {
        level = Mathf.Clamp(level, 1, BusSeat.MaxSupportedSeatLevel);
        GameObject crateObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crateObject.name = $"SeatDeliveryCrate_Lv{level}";
        crateObject.transform.position = position;
        crateObject.transform.rotation = rotation;
        crateObject.transform.localScale = crateSize;

        Renderer crateRenderer = crateObject.GetComponent<Renderer>();
        if (crateRenderer != null)
            crateRenderer.material.color = GetColorForLevel(level);

        Rigidbody crateBody = crateObject.AddComponent<Rigidbody>();
        crateBody.mass = 2.2f;
        crateBody.angularDamping = 3.5f;

        SeatDeliveryCrate crate = crateObject.AddComponent<SeatDeliveryCrate>();
        crate.Setup(level);
        activeCrate = crate;
        return crate;
    }

    Vector3 GetDropPosition()
    {
        Transform dropAnchor = deliveryDropPoint;
        if (dropAnchor == null)
        {
            BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
            dropAnchor = player != null ? player.transform : transform;
        }

        return dropAnchor.position + dropAnchor.TransformVector(deliveryDropOffset);
    }

    Quaternion GetDropRotation()
    {
        Transform dropAnchor = deliveryDropPoint;
        if (dropAnchor == null)
        {
            BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
            dropAnchor = player != null ? player.transform : transform;
        }

        return Quaternion.Euler(0f, dropAnchor.eulerAngles.y, 0f);
    }

    Color GetColorForLevel(int level)
    {
        return level switch
        {
            2 => new Color(0.42f, 0.72f, 0.96f, 1f),
            3 => new Color(0.93f, 0.73f, 0.28f, 1f),
            _ => new Color(0.75f, 0.48f, 0.28f, 1f)
        };
    }
}
