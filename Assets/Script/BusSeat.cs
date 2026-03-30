using UnityEngine;

[ExecuteAlways]
public class BusSeat : MonoBehaviour, IInteractable
{
    public const int MaxSupportedSeatLevel = 3;

    public enum SeatState { Broken, Empty, Usable }
    public enum SeatLevel { None = 0, Lv1 = 1, Lv2 = 2, Lv3 = 3 }

    [Header("สถานะเก้าอี้")]
    public SeatState currentState = SeatState.Broken;
    public SeatLevel currentLevel = SeatLevel.None;

    [Header("ราคา")]
    public int sellPrice = 20;

    [Header("โมเดลแต่ละสถานะ")]
    public GameObject brokenModel;
    public GameObject emptyModel;
    public GameObject goodModel;
    public GameObject goodModelLv1;
    public GameObject goodModelLv2;
    public GameObject goodModelLv3;

    [Header("Save")]
    [SerializeField] string seatId;
    [SerializeField] BoxCollider interactionCollider;
    [SerializeField] BoxCollider solidCollider;

    [Header("Interaction")]
    [SerializeField] Vector3 emptySeatInteractionCenter = new Vector3(0f, 0.42f, 0f);
    [SerializeField] Vector3 emptySeatInteractionSize = new Vector3(0.85f, 0.9f, 0.85f);

    [Header("Passenger Snap")]
    [SerializeField] bool autoAlignPassengerToVisual = true;
    [SerializeField] Vector3 passengerLocalOffset = new Vector3(0f, -0.08f, -0.02f);
    [SerializeField] Vector3 passengerLocalEulerOffset = Vector3.zero;

    GameObject spawnedBrokenModel;
    GameObject spawnedBrokenModelSource;
    GameObject spawnedUsableModel;
    GameObject spawnedUsableModelSource;
    Renderer[] baseRenderers;

    void Start()
    {
        EnsureSeatId();
        NormalizeState();
        SanitizeModelReferences();
        CacheBaseRenderers();
        EnsureInteractionCollider();
        UpdateVisuals();
    }

    void OnEnable()
    {
        SanitizeModelReferences();
        CacheBaseRenderers();
        RefreshBaseRendererVisibility();
    }

    void OnValidate()
    {
        SanitizeModelReferences();
        CacheBaseRenderers();
        RefreshBaseRendererVisibility();
    }

    public void InteractToSell()
    {
        if (currentState != SeatState.Broken || PlayerWallet.Instance == null)
            return;

        if (!PlayerWallet.Instance.SpendMoney(sellPrice))
            return;

        currentState = SeatState.Empty;
        currentLevel = SeatLevel.None;
        UpdateVisuals();
        SaveSystem.SaveCurrentGame();
    }

    public void InstallNewSeat(int level = 1)
    {
        if (currentState != SeatState.Empty)
            return;

        currentState = SeatState.Usable;
        currentLevel = ClampLevel(level);
        UpdateVisuals();
        SaveSystem.SaveCurrentGame();
    }

    public bool IsUsableForPassengers()
    {
        return currentState == SeatState.Usable;
    }

    public Pose GetPassengerSnapPose(Transform fallbackSeatPoint = null)
    {
        if (TryGetPassengerSnapPoseFromVisual(out Pose visualPose))
            return visualPose;

        Transform anchor = fallbackSeatPoint != null ? fallbackSeatPoint : transform;
        Vector3 position = anchor.position + anchor.TransformVector(passengerLocalOffset);
        Quaternion rotation = anchor.rotation * Quaternion.Euler(passengerLocalEulerOffset);
        return new Pose(position, rotation);
    }

    public bool CanInteract()
    {
        if (currentState == SeatState.Broken)
            return true;

        if (currentState != SeatState.Empty)
            return false;

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        return player != null && player.IsCarryingSeatPackage();
    }

    public void Interact()
    {
        if (currentState == SeatState.Broken)
        {
            InteractToSell();
            return;
        }

        SeatDeliveryManager deliveryManager = SeatDeliveryManager.GetOrCreateInstance();
        if (currentState != SeatState.Empty || deliveryManager == null)
            return;

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        if (player == null)
            return;

        deliveryManager.TryInstallCarriedSeat(this, player);
    }

    public string GetPromptText()
    {
        if (currentState == SeatState.Broken)
            return $"กด E เพื่อรื้อเก้าอี้พัง (-{sellPrice} ฿)";

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        if (currentState == SeatState.Empty && player != null && player.IsCarryingSeatPackage())
            return $"กด E เพื่อติดตั้งเก้าอี้ Lv.{player.GetCarriedSeatLevel()}";

        return string.Empty;
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
        SanitizeModelReferences();
        EnsureInteractionCollider();
        EnsureSolidCollider();

        SetSceneObjectActive(brokenModel, currentState == SeatState.Broken);
        SetSceneObjectActive(emptyModel, currentState == SeatState.Empty);

        bool usingLevelModels = goodModelLv1 != null || goodModelLv2 != null || goodModelLv3 != null;
        bool useFallbackGoodModel = currentState == SeatState.Usable &&
            (!usingLevelModels || currentLevel == SeatLevel.Lv1 && goodModelLv1 == null);

        SetSceneObjectActive(goodModel, useFallbackGoodModel);
        SetSceneObjectActive(goodModelLv1, currentState == SeatState.Usable && currentLevel == SeatLevel.Lv1);
        SetSceneObjectActive(goodModelLv2, currentState == SeatState.Usable && currentLevel == SeatLevel.Lv2);
        SetSceneObjectActive(goodModelLv3, currentState == SeatState.Usable && currentLevel == SeatLevel.Lv3);

        RefreshSpawnedBrokenModel();
        RefreshSpawnedUsableModel();
        RefreshBaseRendererVisibility();
        RefreshSolidCollider();
    }

    void EnsureInteractionCollider()
    {
        if (interactionCollider != null)
        {
            interactionCollider.isTrigger = true;
            RefreshInteractionCollider();
            return;
        }

        interactionCollider = GetComponent<BoxCollider>();
        if (interactionCollider == null)
            interactionCollider = gameObject.AddComponent<BoxCollider>();
        interactionCollider.isTrigger = true;

        RefreshInteractionCollider();
    }

    void RefreshInteractionCollider()
    {
        if (interactionCollider == null)
            return;

        if (currentState == SeatState.Empty)
        {
            interactionCollider.enabled = true;
            interactionCollider.center = emptySeatInteractionCenter;
            interactionCollider.size = emptySeatInteractionSize;
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            interactionCollider.center = new Vector3(0f, 0.45f, 0f);
            interactionCollider.size = new Vector3(0.55f, 0.9f, 0.55f);
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                worldBounds.Encapsulate(renderers[i].bounds);
        }

        interactionCollider.center = transform.InverseTransformPoint(worldBounds.center);
        Vector3 lossyScale = transform.lossyScale;
        interactionCollider.size = new Vector3(
            SafeLocalSize(worldBounds.size.x, lossyScale.x),
            SafeLocalSize(worldBounds.size.y, lossyScale.y),
            SafeLocalSize(worldBounds.size.z, lossyScale.z));
    }

    void EnsureSolidCollider()
    {
        if (solidCollider != null)
            return;

        BoxCollider[] colliders = GetComponents<BoxCollider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i] != interactionCollider && !colliders[i].isTrigger)
            {
                solidCollider = colliders[i];
                break;
            }
        }

        if (solidCollider == null)
            solidCollider = gameObject.AddComponent<BoxCollider>();

        solidCollider.isTrigger = false;
    }

    void RefreshSolidCollider()
    {
        if (solidCollider == null)
            return;

        bool shouldBlockMovement = currentState != SeatState.Empty;
        solidCollider.enabled = shouldBlockMovement;
        if (!shouldBlockMovement)
            return;

        Renderer[] renderers = GetSeatVisualRenderers();
        if (renderers.Length == 0)
        {
            solidCollider.center = new Vector3(0f, 0.35f, 0f);
            solidCollider.size = new Vector3(0.7f, 0.75f, 0.7f);
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                worldBounds.Encapsulate(renderers[i].bounds);
        }

        worldBounds.Expand(new Vector3(0.02f, 0.02f, 0.02f));
        solidCollider.center = transform.InverseTransformPoint(worldBounds.center);
        Vector3 lossyScale = transform.lossyScale;
        solidCollider.size = new Vector3(
            SafeLocalSize(worldBounds.size.x, lossyScale.x),
            SafeLocalSize(worldBounds.size.y, lossyScale.y),
            SafeLocalSize(worldBounds.size.z, lossyScale.z));
    }

    Renderer[] GetSeatVisualRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        System.Collections.Generic.List<Renderer> filtered = new System.Collections.Generic.List<Renderer>(renderers.Length);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            SeatPlacementMarker marker = renderer.GetComponent<SeatPlacementMarker>();
            if (marker != null)
                continue;

            filtered.Add(renderer);
        }

        return filtered.ToArray();
    }

    bool TryGetPassengerSnapPoseFromVisual(out Pose pose)
    {
        pose = default;
        if (!autoAlignPassengerToVisual)
            return false;

        Renderer[] renderers = GetSeatVisualRenderers();
        if (renderers.Length == 0)
            return false;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = new Vector3(
            SafeLocalSize(worldBounds.size.x, transform.lossyScale.x),
            SafeLocalSize(worldBounds.size.y, transform.lossyScale.y),
            SafeLocalSize(worldBounds.size.z, transform.lossyScale.z));

        Vector3 localSnap = new Vector3(
            localCenter.x,
            localCenter.y - (localSize.y * 0.1f),
            localCenter.z) + passengerLocalOffset;

        Quaternion rotation = transform.rotation * Quaternion.Euler(passengerLocalEulerOffset);
        pose = new Pose(transform.TransformPoint(localSnap), rotation);
        return true;
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
        level = Mathf.Clamp(level, 1, MaxSupportedSeatLevel);
        if (level <= 1) return SeatLevel.Lv1;
        if (level == 2) return SeatLevel.Lv2;
        return SeatLevel.Lv3;
    }

    float SafeLocalSize(float worldSize, float axisScale)
    {
        float safeScale = Mathf.Abs(axisScale);
        if (safeScale <= 0.0001f)
            return worldSize;

        return worldSize / safeScale;
    }

    void SanitizeModelReferences()
    {
        brokenModel = NormalizeGameObjectReference(brokenModel);
        emptyModel = NormalizeGameObjectReference(emptyModel);
        goodModel = NormalizeGameObjectReference(goodModel);
        goodModelLv1 = NormalizeGameObjectReference(goodModelLv1);
        goodModelLv2 = NormalizeGameObjectReference(goodModelLv2);
        goodModelLv3 = NormalizeGameObjectReference(goodModelLv3);
    }

    GameObject NormalizeGameObjectReference(GameObject target)
    {
        return IsUnityObjectAlive(target) ? target : null;
    }

    void CacheBaseRenderers()
    {
        baseRenderers = GetComponentsInChildren<Renderer>(true);
    }

    void RefreshSpawnedUsableModel()
    {
        if (currentState != SeatState.Usable)
        {
            DestroySpawnedUsableModel();
            return;
        }

        GameObject usableModelSource = GetResolvedUsableModelSource();
        if (usableModelSource == null || IsSceneObject(usableModelSource))
        {
            DestroySpawnedUsableModel();
            return;
        }

        if (spawnedUsableModel != null && spawnedUsableModelSource == usableModelSource)
            return;

        DestroySpawnedUsableModel();

        spawnedUsableModel = InstantiateModelObject(usableModelSource);
        if (spawnedUsableModel == null)
            return;

        spawnedUsableModel.name = $"{usableModelSource.name}_Runtime";
        spawnedUsableModel.transform.localPosition = Vector3.zero;
        spawnedUsableModel.transform.localRotation = Quaternion.identity;
        spawnedUsableModel.transform.localScale = GetSpawnedModelLocalScale();
        spawnedUsableModelSource = usableModelSource;
    }

    void RefreshSpawnedBrokenModel()
    {
        if (currentState != SeatState.Broken)
        {
            DestroySpawnedBrokenModel();
            return;
        }

        GameObject brokenModelSource = GetResolvedBrokenModelSource();
        if (brokenModelSource == null || IsSceneObject(brokenModelSource))
        {
            DestroySpawnedBrokenModel();
            return;
        }

        if (spawnedBrokenModel != null && spawnedBrokenModelSource == brokenModelSource)
            return;

        DestroySpawnedBrokenModel();

        spawnedBrokenModel = InstantiateModelObject(brokenModelSource);
        if (spawnedBrokenModel == null)
            return;

        spawnedBrokenModel.name = $"{brokenModelSource.name}_Runtime";
        spawnedBrokenModel.transform.localPosition = Vector3.zero;
        spawnedBrokenModel.transform.localRotation = Quaternion.identity;
        spawnedBrokenModel.transform.localScale = GetSpawnedModelLocalScale();
        spawnedBrokenModelSource = brokenModelSource;
    }

    void DestroySpawnedUsableModel()
    {
        if (spawnedUsableModel != null)
            Destroy(spawnedUsableModel);

        spawnedUsableModel = null;
        spawnedUsableModelSource = null;
    }

    void DestroySpawnedBrokenModel()
    {
        if (spawnedBrokenModel != null)
            Destroy(spawnedBrokenModel);

        spawnedBrokenModel = null;
        spawnedBrokenModelSource = null;
    }

    void RefreshBaseRendererVisibility()
    {
        if (baseRenderers == null || baseRenderers.Length == 0)
            return;

        bool hasSpawnedVisual = spawnedUsableModel != null || spawnedBrokenModel != null;
        bool hideLegacyInEditor = !Application.isPlaying && UsesExternalVisuals();
        bool showBaseRenderers = currentState != SeatState.Empty && !hasSpawnedVisual && !hideLegacyInEditor;

        for (int i = 0; i < baseRenderers.Length; i++)
        {
            if (baseRenderers[i] == null)
                continue;

            SeatPlacementMarker marker = baseRenderers[i].GetComponent<SeatPlacementMarker>();
            if (marker != null)
            {
                baseRenderers[i].enabled = marker.ShouldShowMarkerVisual();
                continue;
            }

            if (spawnedBrokenModel != null && baseRenderers[i].transform.IsChildOf(spawnedBrokenModel.transform))
                continue;

            if (spawnedUsableModel != null && baseRenderers[i].transform.IsChildOf(spawnedUsableModel.transform))
                continue;

            baseRenderers[i].enabled = showBaseRenderers;
        }
    }

    bool UsesExternalVisuals()
    {
        return IsExternalAsset(GetResolvedBrokenModelSource()) ||
               IsExternalAsset(GetResolvedUsableModelSource());
    }

    GameObject GetResolvedUsableModelSource()
    {
        if (currentLevel == SeatLevel.Lv2 && goodModelLv2 != null)
            return goodModelLv2;

        if (currentLevel == SeatLevel.Lv3 && goodModelLv3 != null)
            return goodModelLv3;

        if (currentLevel == SeatLevel.Lv1 && goodModelLv1 != null)
            return goodModelLv1;

        if (goodModel != null)
            return goodModel;

        BusSeat templateSeat = GetTemplateSeat();
        if (templateSeat != null && templateSeat != this)
            return templateSeat.GetUsableModelSourceForLevel(currentLevel);

        return null;
    }

    GameObject GetUsableModelSourceForLevel(SeatLevel level)
    {
        if (level == SeatLevel.Lv2 && goodModelLv2 != null)
            return goodModelLv2;

        if (level == SeatLevel.Lv3 && goodModelLv3 != null)
            return goodModelLv3;

        if (level == SeatLevel.Lv1 && goodModelLv1 != null)
            return goodModelLv1;

        if (goodModel != null)
            return goodModel;

        return null;
    }

    GameObject GetResolvedBrokenModelSource()
    {
        if (brokenModel != null)
            return brokenModel;

        BusSeat templateSeat = GetTemplateSeat();
        if (templateSeat != null && templateSeat != this)
            return templateSeat.brokenModel;

        return null;
    }

    void SetSceneObjectActive(GameObject target, bool active)
    {
        if (!IsSceneObject(target))
            return;

        target.SetActive(active);
    }

    bool IsSceneObject(GameObject target)
    {
        if (!IsUnityObjectAlive(target))
            return false;

        try
        {
            return target.scene.IsValid();
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    bool IsExternalAsset(GameObject target)
    {
        if (!IsUnityObjectAlive(target))
            return false;

        try
        {
            return !target.scene.IsValid();
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    bool IsUnityObjectAlive(UnityEngine.Object target)
    {
        if (ReferenceEquals(target, null))
            return false;

        try
        {
            return target != null;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    GameObject InstantiateModelObject(GameObject source)
    {
        if (!IsUnityObjectAlive(source))
            return null;

        try
        {
            UnityEngine.Object instance = UnityEngine.Object.Instantiate((UnityEngine.Object)source, transform);
            if (instance is GameObject instanceGameObject)
                return instanceGameObject;

            if (instance is Component instanceComponent)
                return instanceComponent.gameObject;
        }
        catch (System.InvalidCastException)
        {
            Debug.LogWarning($"BusSeat could not instantiate model asset '{source.name}' because the reference type is not a GameObject root.");
        }

        return null;
    }

    BusSeat GetTemplateSeat()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            BusSeat templateSeat = current.GetComponent<BusSeat>();
            if (templateSeat != null)
                return templateSeat;

            current = current.parent;
        }

        return null;
    }

    Vector3 GetSpawnedModelLocalScale()
    {
        SeatPlacementMarker marker = GetComponent<SeatPlacementMarker>();
        if (marker == null || !marker.UsesSelfMarkerVisual)
            return Vector3.one;

        Vector3 localScale = transform.localScale;
        return new Vector3(
            SafeInverseScale(localScale.x),
            SafeInverseScale(localScale.y),
            SafeInverseScale(localScale.z));
    }

    float SafeInverseScale(float value)
    {
        float absValue = Mathf.Abs(value);
        if (absValue <= 0.0001f)
            return 1f;

        return 1f / absValue;
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
