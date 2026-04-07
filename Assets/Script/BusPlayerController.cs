using System;
using UnityEngine;

public class BusPlayerController : MonoBehaviour
{
    [Header("1. ค่าความเร็ว")]
    public float walkSpeed = 2f;
    public float runSpeed = 3f;
    public float exhaustedSpeed = 0.2f;

    [Header("2. ค่า Stamina")]
    public float maxStamina = 10f;
    public float staminaDrainRate = 15f;
    public float normalRegenRate = 3f;
    public float penaltyRegenRate = 0.5f;

    [Header("3. การตั้งค่าอื่นๆ")]
    public float mouseSensitivity = 100f;
    public float gravity = -30.0f;
    public float acceleration = 8f;
    public Transform playerCamera;

    [Header("4. ระบบ Interaction")]
    public float interactRange = 2.0f;
    public float interactSphereRadius = 0.18f;
    public LayerMask interactLayer;
    public InteractPromptUI interactPromptUI;
    public float seatInstallAssistRange = 3.2f;
    public float seatInstallAssistWidth = 0.95f;

    [Header("5. ระบบ Payment UI")]
    public GameObject uiPanel;
    public UnityEngine.UI.Text textPrice;
    public Animator npcAnimator;

    [Header("6. ระบบแรงเหวี่ยง (Inertia)")]
    [Tooltip("ความแรงของการเซ (ยิ่งเยอะ ยิ่งเซแรงจนปลิว)")]
    public float inertiaMultiplier = 10f;
    [Tooltip("ความเร็วในการทรงตัวกลับมายืนตรงๆ (ยิ่งเยอะ ยิ่งหายเซไว)")]
    public float stumbleRecoverySpeed = 5f;

    [Header("7. ระบบการเอียงตัว (Leaning)")]
    [Tooltip("ความเอียงต่อแรงผลัก (ยิ่งเยอะ ยิ่งเอียงเยอะ)")]
    public float leanSensitivity = 2f;
    [Tooltip("องศาการเอียงสูงสุด")]
    public float maxLeanAngle = 20f;

    [Header("8. ระบบฟิสิกส์ตอนลงรถ")]
    public CityManager cityManager;
    public bool isInsideBus = true;
    public Vector3 scrollDirection = new Vector3(-1, 0, 0);

    [Header("9. ระบบถือกล่อง")]
    public Transform carryAnchor;
    public Vector3 carriedItemLocalPosition = new Vector3(0.32f, -0.28f, 0.95f);
    public Vector3 carriedItemLocalEuler = new Vector3(10f, -18f, 6f);

    [Header("10. ระบบถือสเปรย์")]
    public Vector3 carriedSprayLocalPosition = new Vector3(0.42f, -0.34f, 0.7f);
    public Vector3 carriedSprayLocalEuler = new Vector3(8f, -16f, 8f);
    public float sprayUseDuration = 4f;
    public string leftHandObjectName = "Hand_L";
    public string ticketCylinderObjectName = "TicketCylinder";

    CharacterController controller;
    FareSystem fareSystem;
    float lastBusSpeed;
    Vector3 inertiaVelocity;
    float xRotation = 0f;
    Vector3 velocity;
    public float currentStamina;
    float activeMoveSpeed;
    bool isTransactionActive = false;
    const float maxFallSpeed = -20f;
    SeatDeliveryCrate carriedSeatPackage;
    SprayDeliveryItem carriedSprayItem;
    float sprayUseProgress = 0f;
    GameObject leftHandObject;
    GameObject ticketCylinderObject;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        fareSystem = FindFirstObjectByType<FareSystem>();
        EnsureCarryAnchor();
        EnsureLeftHandObject();
        EnsureTicketCylinderObject();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentStamina = maxStamina;
        activeMoveSpeed = walkSpeed;

        if (interactPromptUI != null) interactPromptUI.Hide(true);
        if (cityManager != null) lastBusSpeed = cityManager._currentSpeed;
    }

    void Update()
    {
        if (Cursor.visible) return;

        HandleMouseLook();
        HandleMovement();
        HandleSprayUse();
        HandleInteraction();

        if (!isInsideBus && cityManager != null)
        {
            Vector3 pushMovement = scrollDirection * cityManager._currentSpeed * Time.deltaTime;
            controller.Move(pushMovement);
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 direction = (transform.right * x + transform.forward * z).normalized;
        bool isMoving = direction.magnitude >= 0.1f;
        bool isShiftHold = Input.GetKey(KeyCode.LeftShift);

        float targetSpeed = walkSpeed;
        if (isShiftHold && isMoving)
            targetSpeed = currentStamina > 0 ? runSpeed : exhaustedSpeed;

        activeMoveSpeed = Mathf.Lerp(activeMoveSpeed, targetSpeed, acceleration * Time.deltaTime);

        if (cityManager != null)
        {
            float currentBusSpeed = cityManager._currentSpeed;
            float deltaSpeed = currentBusSpeed - lastBusSpeed;

            if (Mathf.Abs(deltaSpeed) < 2f && Mathf.Abs(deltaSpeed) > 0.001f)
                inertiaVelocity += Vector3.left * (deltaSpeed * inertiaMultiplier);

            lastBusSpeed = currentBusSpeed;
        }

        inertiaVelocity = Vector3.ClampMagnitude(inertiaVelocity, 5f);

        if (cityManager != null)
        {
            float leanAmount = Mathf.Clamp(inertiaVelocity.x * leanSensitivity, -maxLeanAngle, maxLeanAngle);
            float currentYRotation = transform.localEulerAngles.y;
            Quaternion targetLeanRotation = Quaternion.Euler(leanAmount, currentYRotation, 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLeanRotation, 7f * Time.deltaTime);
        }

        inertiaVelocity = Vector3.Lerp(inertiaVelocity, Vector3.zero, stumbleRecoverySpeed * Time.deltaTime);

        Vector3 finalMovement = (direction * activeMoveSpeed) + inertiaVelocity;
        controller.Move(finalMovement * Time.deltaTime);

        if (isShiftHold && isMoving && currentStamina > 0)
            currentStamina -= staminaDrainRate * Time.deltaTime;
        else if (isShiftHold)
            currentStamina += penaltyRegenRate * Time.deltaTime;
        else
            currentStamina += normalRegenRate * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y = Mathf.Max(velocity.y + gravity * Time.deltaTime, maxFallSpeed);
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleInteraction()
    {
        if (IsCarryingSprayItem())
        {
            if (interactPromptUI != null)
                interactPromptUI.Show("คลิกซ้ายเพื่อฉีดสเปรย์  |  กด E เพื่อวางลง", true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                SprayDeliveryManager sprayDeliveryManager = SprayDeliveryManager.GetOrCreateInstance();
                if (sprayDeliveryManager != null)
                    sprayDeliveryManager.TryDropCarriedSpray(this, out _);
            }
            return;
        }

        if (isTransactionActive)
        {
            if (fareSystem == null)
                fareSystem = FindFirstObjectByType<FareSystem>();

            if (fareSystem == null || !fareSystem.HasActiveTransaction())
                ResetInteraction();
            else
                return;
        }

        bool foundInteractable = false;

        if (TryGetInteractable(out IInteractable interactable))
        {
            foundInteractable = true;
            if (interactPromptUI != null)
                interactPromptUI.Show(interactable.GetPromptText());

            if (Input.GetKeyDown(KeyCode.E))
            {
                PassengerAI passenger = interactable as PassengerAI;
                bool shouldLockInteraction = passenger != null && !passenger.CanResolveRandomEvent();
                isTransactionActive = shouldLockInteraction;

                if (interactPromptUI != null)
                    interactPromptUI.Hide();

                interactable.Interact();

                if (!shouldLockInteraction)
                    ResetInteraction();
            }
        }

        if (!foundInteractable && interactPromptUI != null && interactPromptUI.IsVisible)
            interactPromptUI.Hide();
    }

    bool TryGetInteractable(out IInteractable interactable)
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (TryGetInteractableFromHits(Physics.SphereCastAll(ray, interactSphereRadius, interactRange, interactLayer, QueryTriggerInteraction.Ignore), out interactable))
            return true;

        if (TryGetInteractableFromHits(Physics.SphereCastAll(ray, interactSphereRadius, interactRange, ~0, QueryTriggerInteraction.Ignore), out interactable))
            return true;

        return TryGetSeatInstallFallback(ray, out interactable);
    }

    bool TryGetInteractableFromHits(RaycastHit[] hits, out IInteractable interactable)
    {
        interactable = default;
        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            IInteractable candidate = hitCollider.GetComponentInParent<IInteractable>();
            if (candidate == null || !candidate.CanInteract())
                continue;

            interactable = candidate;
            return true;
        }

        return false;
    }

    bool TryGetSeatInstallFallback(Ray ray, out IInteractable interactable)
    {
        interactable = default;
        if (!IsCarryingSeatPackage())
            return false;

        BusSeat[] seats = UnityEngine.Object.FindObjectsByType<BusSeat>(FindObjectsSortMode.None);
        BusSeat bestSeat = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < seats.Length; i++)
        {
            BusSeat seat = seats[i];
            if (seat == null || !seat.CanInteract())
                continue;

            Vector3 targetPosition = seat.transform.position + seat.transform.up * 0.35f;
            Vector3 toTarget = targetPosition - ray.origin;
            float forwardDistance = Vector3.Dot(ray.direction, toTarget);
            if (forwardDistance < 0f || forwardDistance > seatInstallAssistRange)
                continue;

            Vector3 closestPoint = ray.origin + ray.direction * forwardDistance;
            float lateralDistance = Vector3.Distance(closestPoint, targetPosition);
            if (lateralDistance > seatInstallAssistWidth)
                continue;

            float score = lateralDistance + (forwardDistance * 0.05f);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestSeat = seat;
        }

        if (bestSeat == null)
            return false;

        interactable = bestSeat;
        return true;
    }

    public void ResetInteraction()
    {
        isTransactionActive = false;
    }

    public bool IsCarryingSeatPackage()
    {
        return carriedSeatPackage != null;
    }

    public bool IsCarryingSprayItem()
    {
        return carriedSprayItem != null;
    }

    public int GetCarriedSprayRemainingUses()
    {
        return carriedSprayItem != null ? carriedSprayItem.GetRemainingUses() : 0;
    }

    public SprayDeliveryItem GetCarriedSprayItem()
    {
        return carriedSprayItem;
    }

    public int GetCarriedSeatLevel()
    {
        return carriedSeatPackage != null ? carriedSeatPackage.seatLevel : 0;
    }

    public void AttachSeatPackage(SeatDeliveryCrate crate)
    {
        if (crate == null)
            return;

        EnsureCarryAnchor();
        carriedSeatPackage = crate;
        carriedSeatPackage.SetCarriedState(true);
        carriedSeatPackage.transform.SetParent(carryAnchor, false);
        carriedSeatPackage.transform.localPosition = carriedItemLocalPosition;
        carriedSeatPackage.transform.localRotation = Quaternion.Euler(carriedItemLocalEuler);
    }

    public void ClearCarriedSeatPackage()
    {
        if (carriedSeatPackage == null)
            return;

        Destroy(carriedSeatPackage.gameObject);
        carriedSeatPackage = null;
    }

    public void AttachSprayItem(SprayDeliveryItem item)
    {
        if (item == null)
            return;

        EnsureCarryAnchor();
        carriedSprayItem = item;
        sprayUseProgress = 0f;
        carriedSprayItem.SetCarriedState(true);
        carriedSprayItem.transform.SetParent(carryAnchor, false);
        carriedSprayItem.transform.localScale = Vector3.one;
        carriedSprayItem.transform.localPosition = carriedSprayLocalPosition;
        carriedSprayItem.transform.localRotation = Quaternion.Euler(carriedSprayLocalEuler);
        SetLeftHandVisible(false);
        SetTicketCylinderVisible(false);
    }

    public void ClearCarriedSprayItem()
    {
        if (carriedSprayItem == null)
            return;

        Destroy(carriedSprayItem.gameObject);
        carriedSprayItem = null;
        sprayUseProgress = 0f;
        SetLeftHandVisible(true);
        SetTicketCylinderVisible(true);
    }

    public SprayDeliveryItem DropCarriedSprayItem(Vector3 worldPosition, Quaternion worldRotation, bool armDropSound)
    {
        if (carriedSprayItem == null)
            return null;

        SprayDeliveryItem item = carriedSprayItem;
        carriedSprayItem = null;
        sprayUseProgress = 0f;

        item.transform.SetParent(null, true);
        item.transform.localScale = Vector3.one;
        item.transform.position = worldPosition;
        item.transform.rotation = worldRotation;
        item.SetCarriedState(false);
        if (armDropSound)
            item.ArmDropSound();

        SetLeftHandVisible(true);
        SetTicketCylinderVisible(true);
        return item;
    }

    void HandleSprayUse()
    {
        if (!IsCarryingSprayItem())
        {
            sprayUseProgress = 0f;
            return;
        }

        if (!Input.GetMouseButton(0))
            return;

        sprayUseProgress += Time.deltaTime;
        if (sprayUseProgress < sprayUseDuration)
            return;

        SprayDeliveryManager sprayDeliveryManager = SprayDeliveryManager.GetOrCreateInstance();
        if (sprayDeliveryManager != null)
            sprayDeliveryManager.TryUseCarriedSpray(this, out _);

        sprayUseProgress = 0f;
    }

    void EnsureCarryAnchor()
    {
        if (carryAnchor != null)
            return;

        Transform parent = playerCamera != null ? playerCamera : transform;
        GameObject anchor = new GameObject("CarryAnchor");
        anchor.transform.SetParent(parent, false);
        carryAnchor = anchor.transform;
    }

    void EnsureLeftHandObject()
    {
        leftHandObject = FindSceneObjectByName(leftHandObjectName, leftHandObject);
    }

    void EnsureTicketCylinderObject()
    {
        ticketCylinderObject = FindSceneObjectByName(ticketCylinderObjectName, ticketCylinderObject);
    }

    GameObject FindSceneObjectByName(string objectName, GameObject cachedObject)
    {
        if (cachedObject != null || string.IsNullOrWhiteSpace(objectName))
            return cachedObject;

        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != objectName)
                continue;

            if (!candidate.gameObject.scene.IsValid())
                continue;

            return candidate.gameObject;
        }

        return null;
    }

    void SetLeftHandVisible(bool isVisible)
    {
        EnsureLeftHandObject();
        if (leftHandObject == null)
            return;

        if (leftHandObject.activeSelf != isVisible)
            leftHandObject.SetActive(isVisible);
    }

    void SetTicketCylinderVisible(bool isVisible)
    {
        EnsureTicketCylinderObject();
        if (ticketCylinderObject == null)
            return;

        if (ticketCylinderObject.activeSelf != isVisible)
            ticketCylinderObject.SetActive(isVisible);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("BusZone"))
            isInsideBus = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("BusZone"))
            isInsideBus = false;
    }
}
