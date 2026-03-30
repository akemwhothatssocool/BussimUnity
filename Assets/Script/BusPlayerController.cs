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

    CharacterController controller;
    float lastBusSpeed;
    Vector3 inertiaVelocity;
    float xRotation = 0f;
    Vector3 velocity;
    public float currentStamina;
    float activeMoveSpeed;
    bool isTransactionActive = false;
    const float maxFallSpeed = -20f;
    SeatDeliveryCrate carriedSeatPackage;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        EnsureCarryAnchor();
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
        if (isTransactionActive) return;

        bool foundInteractable = false;

        if (TryGetInteractable(out IInteractable interactable))
        {
            foundInteractable = true;
            if (interactPromptUI != null)
                interactPromptUI.Show(interactable.GetPromptText());

            if (Input.GetKeyDown(KeyCode.E))
            {
                bool shouldLockInteraction = interactable is PassengerAI;
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

        if (TryGetInteractableFromHits(Physics.RaycastAll(ray, interactRange, interactLayer), out interactable))
            return true;

        if (TryGetInteractableFromHits(Physics.RaycastAll(ray, interactRange), out interactable))
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

    void EnsureCarryAnchor()
    {
        if (carryAnchor != null)
            return;

        Transform parent = playerCamera != null ? playerCamera : transform;
        GameObject anchor = new GameObject("CarryAnchor");
        anchor.transform.SetParent(parent, false);
        carryAnchor = anchor.transform;
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
