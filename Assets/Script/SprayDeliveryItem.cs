using UnityEngine;

public class SprayDeliveryItem : MonoBehaviour, IInteractable
{
    const string BoxDropClipResourcePath = "Sound/BoxDrop";

    static AudioClip cachedBoxDropClip;
    [SerializeField] float boxDropVolume = 0.45f;
    [SerializeField] float minDropImpactSpeed = 0.55f;

    Rigidbody itemBody;
    Collider itemCollider;
    bool playDropSoundOnNextCollision;
    bool hasPlayedDropSound;
    int remainingUses;

    void Awake()
    {
        itemBody = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
    }

    public void Setup(GameObject visualPrefab, Vector3 visualLocalScale, Vector3 visualLocalEuler, int initialUses)
    {
        name = "SprayDeliveryItem";
        remainingUses = Mathf.Max(1, initialUses);

        if (visualPrefab != null)
        {
            GameObject visualInstance = Instantiate(visualPrefab, transform);
            visualInstance.name = "SprayVisual";
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.Euler(visualLocalEuler);
            visualInstance.transform.localScale = visualLocalScale;
            SetLayerRecursively(visualInstance, gameObject.layer);
            return;
        }

        GameObject fallbackVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fallbackVisual.name = "SprayVisualFallback";
        fallbackVisual.transform.SetParent(transform, false);
        fallbackVisual.transform.localPosition = Vector3.zero;
        fallbackVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        fallbackVisual.transform.localScale = new Vector3(0.12f, 0.26f, 0.12f);
        Collider fallbackCollider = fallbackVisual.GetComponent<Collider>();
        if (fallbackCollider != null)
            Destroy(fallbackCollider);

        Renderer fallbackRenderer = fallbackVisual.GetComponent<Renderer>();
        if (fallbackRenderer != null)
            fallbackRenderer.material.color = new Color(0.42f, 0.86f, 0.72f, 1f);
    }

    public int GetRemainingUses()
    {
        return remainingUses;
    }

    public int ConsumeUse()
    {
        remainingUses = Mathf.Max(0, remainingUses - 1);
        return remainingUses;
    }

    public void ArmDropSound()
    {
        playDropSoundOnNextCollision = true;
        hasPlayedDropSound = false;
    }

    public bool CanInteract()
    {
        if (transform.parent != null)
            return false;

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        return player != null && !player.IsCarryingSeatPackage() && !player.IsCarryingSprayItem();
    }

    public void Interact()
    {
        SprayDeliveryManager manager = SprayDeliveryManager.Instance;
        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        if (manager == null || player == null)
            return;

        manager.TryPickUpSpray(this, player);
    }

    public string GetPromptText()
    {
        return "กด E เพื่อหยิบสเปรย์";
    }

    public void SetCarriedState(bool carried)
    {
        if (itemBody != null)
        {
            itemBody.isKinematic = carried;
            itemBody.useGravity = !carried;
            itemBody.linearVelocity = Vector3.zero;
            itemBody.angularVelocity = Vector3.zero;
        }

        if (itemCollider != null)
            itemCollider.enabled = !carried;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        gameObject.layer = ignoreRaycastLayer >= 0 && carried ? ignoreRaycastLayer : 0;

        if (carried)
            playDropSoundOnNextCollision = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!playDropSoundOnNextCollision || hasPlayedDropSound)
            return;

        if (collision == null || collision.relativeVelocity.magnitude < minDropImpactSpeed)
            return;

        AudioClip clip = GetBoxDropClip();
        if (clip == null)
            return;

        Vector3 soundPosition = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        AudioSource.PlayClipAtPoint(clip, soundPosition, boxDropVolume);
        hasPlayedDropSound = true;
        playDropSoundOnNextCollision = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void PrewarmBoxDropClip()
    {
        GetBoxDropClip();
    }

    static AudioClip GetBoxDropClip()
    {
        if (cachedBoxDropClip == null)
            cachedBoxDropClip = Resources.Load<AudioClip>(BoxDropClipResourcePath);

        if (cachedBoxDropClip != null && !cachedBoxDropClip.preloadAudioData && !cachedBoxDropClip.loadState.Equals(AudioDataLoadState.Loaded))
            cachedBoxDropClip.LoadAudioData();

        return cachedBoxDropClip;
    }

    static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;
        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
            SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
    }

    void OnDestroy()
    {
        if (SprayDeliveryManager.Instance != null)
            SprayDeliveryManager.Instance.NotifyItemDestroyed(this);
    }
}
