using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SprayDeliveryManager : MonoBehaviour
{
    const string SprayModelAssetPath = "Assets/Object/Spray.fbx";

    public static SprayDeliveryManager Instance;

    [Header("Delivery Spawn")]
    public Vector3 deliveryDropOffset = new Vector3(0f, 2.1f, 1.25f);
    public Vector3 itemSize = new Vector3(0.42f, 0.42f, 0.42f);
    public float dropSpacePadding = 0.08f;

    [Header("Hand Drop")]
    public Vector3 carriedDropOffset = new Vector3(0.32f, 0f, 0.48f);

    [Header("Spray Visual")]
    public Vector3 sprayVisualLocalScale = new Vector3(0.8f, 0.8f, 0.8f);
    public Vector3 sprayVisualLocalEuler = new Vector3(0f, 0f, 0f);
    public int sprayUsesPerItem = 5;

    SprayDeliveryItem activeItem;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public static SprayDeliveryManager GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        SprayDeliveryManager existing = Object.FindFirstObjectByType<SprayDeliveryManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject managerObject = new GameObject("SprayDeliveryManager");
        Instance = managerObject.AddComponent<SprayDeliveryManager>();
        return Instance;
    }

    public bool TryOrderSprayDelivery(int cost, out string feedback)
    {
        if (cost <= 0)
        {
            feedback = "<color=red>ราคาสเปรย์ไม่ถูกต้อง</color>";
            return false;
        }

        if (PlayerWallet.Instance == null)
        {
            feedback = "<color=red>ไม่พบกระเป๋าเงินผู้เล่น</color>";
            return false;
        }

        if (HasPendingDelivery())
        {
            feedback = "<color=yellow>มีสเปรย์ที่สั่งไว้แล้ว ไปหยิบก่อน</color>";
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
            feedback = "<color=red>ตัดเงินค่าสเปรย์ไม่สำเร็จ</color>";
            return false;
        }

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        bool foundReachableDropPose = TryGetReachableDropPose(player, out Vector3 dropPosition, out Quaternion dropRotation);
        SprayDeliveryItem item = SpawnDeliveryItem(dropPosition, dropRotation, true, sprayUsesPerItem);

        if (!foundReachableDropPose && item != null && player != null && !player.IsCarryingSprayItem())
            TryPickUpSpray(item, player);

        SaveSystem.SaveCurrentGame();

        feedback = foundReachableDropPose
            ? "<color=green>สั่งสเปรย์แล้ว ของถูกวางไว้ใกล้ตัวแล้ว รีบไปหยิบมาใช้งาน</color>"
            : "<color=green>สั่งสเปรย์แล้ว พื้นที่แคบ ระบบเลยส่งสเปรย์เข้ามือให้แทน</color>";
        return true;
    }

    public bool TryPickUpSpray(SprayDeliveryItem item, BusPlayerController player)
    {
        if (item == null || player == null || player.IsCarryingSeatPackage() || player.IsCarryingSprayItem())
            return false;

        activeItem = null;
        player.AttachSprayItem(item);
        SaveSystem.SaveCurrentGame();
        return true;
    }

    public bool TryUseCarriedSpray(BusPlayerController player, out string feedback)
    {
        feedback = string.Empty;
        if (player == null || !player.IsCarryingSprayItem())
            return false;

        SprayDeliveryItem carriedItem = player.GetCarriedSprayItem();
        if (carriedItem == null)
            return false;

        int clearedCount = ResolveToxicPassengers();
        int remainingUses = carriedItem.ConsumeUse();
        if (remainingUses <= 0)
            player.ClearCarriedSprayItem();

        SaveSystem.SaveCurrentGame();

        if (clearedCount > 0)
            feedback = remainingUses > 0
                ? $"<color=green>ฉีดสเปรย์สำเร็จ กลิ่นไม่พึงประสงค์หายไป {clearedCount} จุดแล้ว เหลือใช้อีก {remainingUses} ครั้ง</color>"
                : $"<color=green>ฉีดสเปรย์สำเร็จ กลิ่นไม่พึงประสงค์หายไป {clearedCount} จุดแล้ว และสเปรย์หมดพอดี</color>";
        else
            feedback = remainingUses > 0
                ? $"<color=#7A6B57>ฉีดสเปรย์แล้ว แต่ตอนนี้ยังไม่มีคนตัวเหม็นบนรถ เหลือใช้อีก {remainingUses} ครั้ง</color>"
                : "<color=#7A6B57>ฉีดสเปรย์แล้ว แต่ตอนนี้ยังไม่มีคนตัวเหม็นบนรถ และสเปรย์หมดพอดี</color>";
        return true;
    }

    public bool TryDropCarriedSpray(BusPlayerController player, out string feedback)
    {
        feedback = string.Empty;
        if (player == null || !player.IsCarryingSprayItem())
            return false;

        if (!TryGetCarriedDropPose(player, out Vector3 dropPosition, out Quaternion dropRotation))
        {
            feedback = "<color=yellow>ตรงนี้แคบไป วางสเปรย์ไม่ถนัด ลองขยับอีกนิด</color>";
            return false;
        }

        SprayDeliveryItem droppedItem = player.DropCarriedSprayItem(dropPosition, dropRotation, true);
        if (droppedItem == null)
            return false;

        activeItem = droppedItem;
        SaveSystem.SaveCurrentGame();
        feedback = "<color=green>วางสเปรย์ลงพื้นแล้ว</color>";
        return true;
    }

    public bool HasPendingDelivery()
    {
        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        return activeItem != null || (player != null && player.IsCarryingSprayItem());
    }

    public void CaptureSaveData(GameSaveData data)
    {
        if (data == null)
            return;

        data.hasPendingSprayDelivery = activeItem != null;
        data.pendingSprayDeliveryPosition = activeItem != null ? activeItem.transform.position : Vector3.zero;
        data.pendingSprayDeliveryRotation = activeItem != null ? activeItem.transform.eulerAngles : Vector3.zero;
        data.pendingSprayRemainingUses = activeItem != null ? activeItem.GetRemainingUses() : 0;

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        data.isCarryingSprayDelivery = player != null && player.IsCarryingSprayItem();
        data.carriedSprayRemainingUses = player != null ? player.GetCarriedSprayRemainingUses() : 0;
    }

    public void ApplySaveData(GameSaveData data)
    {
        ClearRuntimeState();
        if (data == null)
            return;

        if (data.hasPendingSprayDelivery)
            SpawnDeliveryItem(
                data.pendingSprayDeliveryPosition,
                Quaternion.Euler(data.pendingSprayDeliveryRotation),
                false,
                data.pendingSprayRemainingUses > 0 ? data.pendingSprayRemainingUses : sprayUsesPerItem);

        if (data.isCarryingSprayDelivery)
        {
            SprayDeliveryItem carriedItem = SpawnDeliveryItem(
                Vector3.zero,
                Quaternion.identity,
                false,
                data.carriedSprayRemainingUses > 0 ? data.carriedSprayRemainingUses : sprayUsesPerItem);
            BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
            if (player != null && carriedItem != null)
                TryPickUpSpray(carriedItem, player);
        }
    }

    public void NotifyItemDestroyed(SprayDeliveryItem item)
    {
        if (activeItem == item)
            activeItem = null;
    }

    void ClearRuntimeState()
    {
        if (activeItem != null)
        {
            Destroy(activeItem.gameObject);
            activeItem = null;
        }

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        if (player != null && player.IsCarryingSprayItem())
            player.ClearCarriedSprayItem();
    }

    SprayDeliveryItem SpawnDeliveryItem(Vector3 position, Quaternion rotation, bool armDropSound, int remainingUses)
    {
        GameObject itemObject = new GameObject("SprayDeliveryItem");
        itemObject.transform.position = position;
        itemObject.transform.rotation = rotation;

        Rigidbody itemBody = itemObject.AddComponent<Rigidbody>();
        itemBody.mass = 1.1f;
        itemBody.angularDamping = 3.5f;
        itemBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        itemObject.AddComponent<BusExteriorScrollObject>();

        BoxCollider itemCollider = itemObject.AddComponent<BoxCollider>();
        itemCollider.size = itemSize;

        SprayDeliveryItem item = itemObject.AddComponent<SprayDeliveryItem>();
        item.Setup(
            GetSprayVisualPrefab(),
            sprayVisualLocalScale,
            sprayVisualLocalEuler,
            remainingUses > 0 ? remainingUses : sprayUsesPerItem);
        if (armDropSound)
            item.ArmDropSound();

        activeItem = item;
        return item;
    }

    int ResolveToxicPassengers()
    {
        PassengerAI[] passengers = Object.FindObjectsByType<PassengerAI>(FindObjectsSortMode.None);
        if (passengers == null || passengers.Length == 0)
            return 0;

        int clearedCount = 0;
        for (int i = 0; i < passengers.Length; i++)
        {
            PassengerAI passenger = passengers[i];
            if (passenger != null && passenger.TryResolveToxicSmellBySpray())
                clearedCount++;
        }

        return clearedCount;
    }

    GameObject GetSprayVisualPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(SprayModelAssetPath);
#else
        return null;
#endif
    }

    bool TryGetReachableDropPose(BusPlayerController player, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (player == null)
            return false;

        Transform anchor = player.transform;
        rotation = Quaternion.Euler(0f, anchor.eulerAngles.y, 0f);
        float floorHeight = Mathf.Max((itemSize.y * 0.5f) + dropSpacePadding, 0.35f);
        Vector3 basePosition = anchor.position + Vector3.up * floorHeight;
        Vector3 forward = Vector3.ProjectOnPlane(anchor.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(anchor.right, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;

        Vector3[] candidateOffsets =
        {
            anchor.TransformVector(deliveryDropOffset),
            (forward * 1.1f) + (Vector3.up * 1.1f),
            (-forward * 0.95f),
            (right * 0.95f),
            (-right * 0.95f),
            ((-forward + right * 0.35f).normalized * 0.95f),
            ((-forward - right * 0.35f).normalized * 0.95f),
            Vector3.zero
        };

        for (int i = 0; i < candidateOffsets.Length; i++)
        {
            Vector3 candidate = i < 2
                ? anchor.position + candidateOffsets[i]
                : basePosition + candidateOffsets[i];

            if (!IsDropSpaceClear(candidate, rotation, player))
                continue;

            position = candidate;
            return true;
        }

        position = basePosition;
        return false;
    }

    bool TryGetCarriedDropPose(BusPlayerController player, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (player == null)
            return false;

        Transform anchor = player.transform;
        rotation = Quaternion.Euler(0f, anchor.eulerAngles.y, 0f);
        float floorHeight = Mathf.Max((itemSize.y * 0.5f) + dropSpacePadding, 0.35f);
        Vector3 basePosition = anchor.position + Vector3.up * floorHeight;
        Vector3 forward = Vector3.ProjectOnPlane(anchor.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(anchor.right, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;

        Vector3[] candidateOffsets =
        {
            (forward * carriedDropOffset.z) + (right * carriedDropOffset.x),
            (forward * carriedDropOffset.z) - (right * carriedDropOffset.x),
            forward * 0.42f,
            (forward * 0.3f) + (right * 0.5f),
            (forward * 0.3f) - (right * 0.5f),
            right * 0.55f,
            -right * 0.55f
        };

        for (int i = 0; i < candidateOffsets.Length; i++)
        {
            Vector3 candidate = basePosition + candidateOffsets[i];
            if (!IsDropSpaceClear(candidate, rotation, player))
                continue;

            position = candidate;
            return true;
        }

        return false;
    }

    bool IsDropSpaceClear(Vector3 position, Quaternion rotation, BusPlayerController player)
    {
        Vector3 halfExtents = (itemSize * 0.5f) + Vector3.one * dropSpacePadding;
        Collider[] overlaps = Physics.OverlapBox(position, halfExtents, rotation, ~0, QueryTriggerInteraction.Ignore);
        if (overlaps == null || overlaps.Length == 0)
            return true;

        Transform playerRoot = player != null ? player.transform : null;
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null)
                continue;

            if (playerRoot != null && overlap.transform.IsChildOf(playerRoot))
                continue;

            return false;
        }

        return true;
    }
}
