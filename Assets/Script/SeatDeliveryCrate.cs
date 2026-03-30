using UnityEngine;

public class SeatDeliveryCrate : MonoBehaviour, IInteractable
{
    public int seatLevel = 1;

    Rigidbody crateBody;
    Collider crateCollider;

    void Awake()
    {
        crateBody = GetComponent<Rigidbody>();
        crateCollider = GetComponent<Collider>();
    }

    public void Setup(int level)
    {
        seatLevel = Mathf.Clamp(level, 1, 3);
        name = $"SeatDeliveryCrate_Lv{seatLevel}";
    }

    public bool CanInteract()
    {
        if (transform.parent != null)
            return false;

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        return player != null && !player.IsCarryingSeatPackage();
    }

    public void Interact()
    {
        SeatDeliveryManager manager = SeatDeliveryManager.Instance;
        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        if (manager == null || player == null)
            return;

        manager.TryPickUpCrate(this, player);
    }

    public string GetPromptText()
    {
        return $"กด E เพื่อหยิบกล่องเก้าอี้ Lv.{seatLevel}";
    }

    public void SetCarriedState(bool carried)
    {
        if (crateBody != null)
        {
            crateBody.isKinematic = carried;
            crateBody.useGravity = !carried;
            crateBody.linearVelocity = Vector3.zero;
            crateBody.angularVelocity = Vector3.zero;
        }

        if (crateCollider != null)
            crateCollider.enabled = !carried;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        gameObject.layer = ignoreRaycastLayer >= 0 && carried ? ignoreRaycastLayer : 0;
    }

    void OnDestroy()
    {
        if (SeatDeliveryManager.Instance != null)
            SeatDeliveryManager.Instance.NotifyCrateDestroyed(this);
    }
}
