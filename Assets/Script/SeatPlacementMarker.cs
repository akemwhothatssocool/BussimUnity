using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BusSeat))]
public class SeatPlacementMarker : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] bool countsAsSeatSlot = true;

    [Header("Layout")]
    [SerializeField] bool generateSlotsFromChildVisuals;

    [Header("Marker Visual")]
    [SerializeField] bool useSelfMarkerVisual;
    public Vector3 markerOffset = new Vector3(0f, 0.42f, 0f);
    public Vector3 markerSize = new Vector3(0.6f, 0.08f, 0.6f);
    public Color markerColor = new Color(0.35f, 1f, 0.45f, 0.72f);

    const string MarkerChildName = "SeatPlacementVisual";

    Transform markerVisual;
    Renderer markerRenderer;
    BusSeat seat;

    public bool CountsAsSeatSlot => countsAsSeatSlot && !generateSlotsFromChildVisuals;
    public bool UsesSelfMarkerVisual => useSelfMarkerVisual;

    public static void EnsureAllGeneratedSlots()
    {
        SeatPlacementMarker[] markers = Object.FindObjectsByType<SeatPlacementMarker>(FindObjectsSortMode.None);
        if (markers == null || markers.Length == 0)
            return;

        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] == null)
                continue;

            markers[i].SyncGeneratedSlots();
            markers[i].EnsureReferences();
            markers[i].EnsureMarkerVisual();
            markers[i].RefreshMarkerVisual();
        }
    }

    void OnEnable()
    {
        SyncGeneratedSlots();
        EnsureReferences();
        EnsureMarkerVisual();
        RefreshMarkerVisual();
    }

    void OnValidate()
    {
        SyncGeneratedSlots();
        EnsureReferences();
        EnsureMarkerVisual();
        RefreshMarkerVisual();
    }

    void Update()
    {
        if (generateSlotsFromChildVisuals)
            SyncGeneratedSlots();

        RefreshMarkerVisual();
    }

    void EnsureReferences()
    {
        if (seat == null)
            seat = GetComponent<BusSeat>();

        if (useSelfMarkerVisual)
        {
            markerVisual = transform;
            markerRenderer = GetComponent<Renderer>();
            return;
        }

        if (markerVisual == null)
        {
            Transform found = transform.Find(MarkerChildName);
            if (found != null)
            {
                markerVisual = found;
                markerRenderer = found.GetComponent<Renderer>();
            }
        }
    }

    void EnsureMarkerVisual()
    {
        if (!CountsAsSeatSlot)
        {
            HideMarkerVisual();
            return;
        }

        if (useSelfMarkerVisual)
        {
            markerVisual = transform;
            if (markerRenderer == null)
                markerRenderer = GetComponent<Renderer>();

            ApplyVisualStyle();
            return;
        }

        if (markerVisual != null)
        {
            ApplyVisualStyle();
            return;
        }

        GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        markerObject.name = MarkerChildName;
        markerObject.transform.SetParent(transform, false);
        markerVisual = markerObject.transform;
        markerRenderer = markerObject.GetComponent<Renderer>();

        Collider markerCollider = markerObject.GetComponent<Collider>();
        if (markerCollider != null)
        {
            if (Application.isPlaying) Destroy(markerCollider);
            else DestroyImmediate(markerCollider);
        }

        ApplyVisualStyle();
    }

    void ApplyVisualStyle()
    {
        if (markerVisual == null)
            return;

        if (!useSelfMarkerVisual)
        {
            markerVisual.localPosition = markerOffset;
            markerVisual.localRotation = Quaternion.identity;
            markerVisual.localScale = markerSize;
        }

        if (markerRenderer == null)
            markerRenderer = markerVisual.GetComponent<Renderer>();

        if (markerRenderer != null)
        {
            Material material = markerRenderer.sharedMaterial;
            if (material == null || material.name == "Default-Material")
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = "SeatPlacementMarkerMaterial";
                markerRenderer.sharedMaterial = material;
            }

            material.color = markerColor;
        }
    }

    void RefreshMarkerVisual()
    {
        if (markerVisual == null || seat == null)
            return;

        bool shouldShow = ShouldShowMarkerVisual();
        SetMarkerVisible(shouldShow);
    }

    public bool ShouldShowMarkerVisual()
    {
        if (!CountsAsSeatSlot || seat == null || seat.currentState != BusSeat.SeatState.Empty)
            return false;

        if (!Application.isPlaying)
            return true;

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        return player != null && player.IsCarryingSeatPackage();
    }

    void SyncGeneratedSlots()
    {
        EnsureReferences();

        if (!generateSlotsFromChildVisuals)
        {
            ApplyContainerSeatState(false);
            return;
        }

        countsAsSeatSlot = false;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null || !IsLayoutVisual(child))
                continue;

            CreateSlotFromLayoutVisual(child);
        }

        SyncChildSlotsFromTemplate();
        ApplyContainerSeatState(true);
    }

    bool IsLayoutVisual(Transform child)
    {
        if (child == null)
            return false;

        if (!child.name.StartsWith(MarkerChildName))
            return false;

        if (child.GetComponent<SeatPlacementMarker>() != null)
            return false;

        if (child.GetComponent<BusSeat>() != null)
            return false;

        return true;
    }

    void CreateSlotFromLayoutVisual(Transform layoutVisual)
    {
        GameObject slotObject = layoutVisual.gameObject;

        BusSeat slotSeat = slotObject.GetComponent<BusSeat>();
        if (slotSeat == null)
            slotSeat = slotObject.AddComponent<BusSeat>();
        CopySeatTemplate(slotSeat);

        SeatPlacementMarker slotMarker = slotObject.GetComponent<SeatPlacementMarker>();
        if (slotMarker == null)
            slotMarker = slotObject.AddComponent<SeatPlacementMarker>();

        slotMarker.countsAsSeatSlot = true;
        slotMarker.generateSlotsFromChildVisuals = false;
        slotMarker.useSelfMarkerVisual = true;
        slotMarker.markerOffset = Vector3.zero;
        slotMarker.markerSize = layoutVisual.localScale;
        slotMarker.markerColor = markerColor;
        slotMarker.EnsureReferences();
        slotMarker.EnsureMarkerVisual();
        slotMarker.RefreshMarkerVisual();
        slotSeat.UpdateVisuals();
    }

    void SyncChildSlotsFromTemplate()
    {
        if (seat == null)
            return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
                continue;

            SeatPlacementMarker childMarker = child.GetComponent<SeatPlacementMarker>();
            BusSeat childSeat = child.GetComponent<BusSeat>();
            if (childMarker == null || childSeat == null || !childMarker.CountsAsSeatSlot)
                continue;

            ApplyTemplateValues(childSeat, preserveState: true);
            childSeat.UpdateVisuals();
        }
    }

    void ApplyContainerSeatState(bool isLayoutContainer)
    {
        if (seat == null)
            return;

        seat.enabled = !isLayoutContainer;

        BoxCollider collider = seat.GetComponent<BoxCollider>();
        if (collider != null)
            collider.enabled = !isLayoutContainer;

        if (isLayoutContainer)
        {
            seat.currentState = BusSeat.SeatState.Empty;
            seat.currentLevel = BusSeat.SeatLevel.None;
            seat.UpdateVisuals();
            HideMarkerVisual();
        }
    }

    void HideMarkerVisual()
    {
        SetMarkerVisible(false);
    }

    void SetMarkerVisible(bool visible)
    {
        if (markerVisual == null)
            return;

        if (useSelfMarkerVisual)
        {
            if (markerRenderer != null)
                markerRenderer.enabled = visible;

            return;
        }

        if (markerVisual.gameObject.activeSelf != visible)
            markerVisual.gameObject.SetActive(visible);
    }

    void CopySeatTemplate(BusSeat targetSeat)
    {
        if (seat == null || targetSeat == null)
            return;

        ApplyTemplateValues(targetSeat, preserveState: false);
    }

    void ApplyTemplateValues(BusSeat targetSeat, bool preserveState)
    {
        if (targetSeat == null)
            return;

        if (!preserveState)
        {
            targetSeat.currentState = BusSeat.SeatState.Empty;
            targetSeat.currentLevel = BusSeat.SeatLevel.None;
        }
        targetSeat.sellPrice = seat.sellPrice;
        targetSeat.brokenModel = seat.brokenModel;
        targetSeat.emptyModel = seat.emptyModel;
        targetSeat.goodModel = seat.goodModel;
        targetSeat.goodModelLv1 = seat.goodModelLv1;
        targetSeat.goodModelLv2 = seat.goodModelLv2;
        targetSeat.goodModelLv3 = seat.goodModelLv3;
    }
}
