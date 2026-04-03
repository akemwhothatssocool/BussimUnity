using UnityEngine;

public class SeatDeliveryCrate : MonoBehaviour, IInteractable
{
    const string BoxDropClipResourcePath = "Sound/BoxDrop";

    static AudioClip cachedBoxDropClip;
    public int seatLevel = 1;
    [SerializeField] float boxDropVolume = 0.57f;
    [SerializeField] float minDropImpactSpeed = 0.55f;

    Rigidbody crateBody;
    Collider crateCollider;
    bool playDropSoundOnNextCollision;
    bool hasPlayedDropSound;

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

    void OnDestroy()
    {
        if (SeatDeliveryManager.Instance != null)
            SeatDeliveryManager.Instance.NotifyCrateDestroyed(this);
    }
}
