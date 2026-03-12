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

    [Header("5. ระบบ Payment UI")]
    public GameObject uiPanel;
    public UnityEngine.UI.Text textPrice;
    public Animator npcAnimator;

    [Header("6. ระบบแรงเหวี่ยง (Inertia)")]
    public CityManager cityManager;
    [Tooltip("ความแรงของการเซ (ยิ่งเยอะ ยิ่งเซแรงจนปลิว)")]
    public float inertiaMultiplier = 10f;
    [Tooltip("ความเร็วในการทรงตัวกลับมายืนตรงๆ (ยิ่งเยอะ ยิ่งหายเซไว)")]
    public float stumbleRecoverySpeed = 5f;

    // 🌟 ส่วนที่เพิ่มเข้ามาใหม่: ระบบการเอียงตัว (Leaning)
    [Header("7. ระบบการเอียงตัว (Leaning)")]
    [Tooltip("ความเอียงต่อแรงผลัก (ยิ่งเยอะ ยิ่งเอียงเยอะ)")]
    public float leanSensitivity = 2f;
    [Tooltip("องศาการเอียงสูงสุด (เข็มขัดนิรภัย: ล็อคไม่ให้เอียงจนจมพื้น)")]
    public float maxLeanAngle = 20f;
    // ------------------------------------------

    private float lastBusSpeed;
    private Vector3 inertiaVelocity;
    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 velocity;
    public float currentStamina;
    private float activeMoveSpeed;
    private bool isTransactionActive = false;
    private const float maxFallSpeed = -20f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
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

        float targetSpeed;
        if (isShiftHold && isMoving)
            targetSpeed = (currentStamina > 0) ? runSpeed : exhaustedSpeed;
        else
            targetSpeed = walkSpeed;

        activeMoveSpeed = Mathf.Lerp(activeMoveSpeed, targetSpeed, acceleration * Time.deltaTime);

        // ==========================================
        // 8. คำนวณแรงเฉื่อย (เซไปเซมา) แบบล็อคความเร็วไม่ให้วาร์ป!
        // ==========================================
        if (cityManager != null)
        {
            float currentBusSpeed = cityManager._currentSpeed;
            float deltaSpeed = currentBusSpeed - lastBusSpeed;

            if (Mathf.Abs(deltaSpeed) < 2f && Mathf.Abs(deltaSpeed) > 0.001f)
            {
                // ✅ แก้ตรงนี้: เปลี่ยน Vector3.right เป็น Vector3.left 
                // เพื่อกลับทิศทางแรงเหวี่ยง เบรก=หน้า, เร่ง=หลัง
                inertiaVelocity += Vector3.left * (deltaSpeed * inertiaMultiplier);
            }
            lastBusSpeed = currentBusSpeed;
        }

        // เข็มขัดนิรภัย: ล็อคความเร็วสูงสุดของการเซ!
        inertiaVelocity = Vector3.ClampMagnitude(inertiaVelocity, 5f);

        // ==========================================
        // 🌟 9. คำนวณการเอียงตัว (Leaning/Pitch) เพื่อความเนียนตา!
        // ==========================================
        if (cityManager != null)
        {
            float leanAmount = inertiaVelocity.x * leanSensitivity;
            leanAmount = Mathf.Clamp(leanAmount, -maxLeanAngle, maxLeanAngle);

            // ✅ จุดที่แก้บั๊กหันหน้า: เราต้อง "ดึงองศาการหันซ้าย-ขวาเดิม (แกน Y)" มาใส่ไว้ด้วย ไม่บังคับเป็น 0 แล้ว
            float currentYRotation = transform.localEulerAngles.y;

            // สร้างเป้าหมาย: เอียงแกน X ตามแรงเบรก + หันแกน Y ตามเมาส์ปกติ + แกน Z ไม่ต้องเอียง
            Quaternion targetLeanRotation = Quaternion.Euler(leanAmount, currentYRotation, 0f);

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLeanRotation, 7f * Time.deltaTime);
        }
        // ==========================================

        // ค่อยๆ ลดแรงเหวี่ยงลง เหมือนคนพยายามดึงสติกลับมาทรงตัว
        inertiaVelocity = Vector3.Lerp(inertiaVelocity, Vector3.zero, stumbleRecoverySpeed * Time.deltaTime);
        // ==========================================

        // เอาแรงเดินปกติ มารวมกับ แรงเซ
        Vector3 finalMovement = (direction * activeMoveSpeed) + inertiaVelocity;

        controller.Move(finalMovement * Time.deltaTime);

        // ระบบ Stamina
        if (isShiftHold && isMoving && currentStamina > 0)
            currentStamina -= staminaDrainRate * Time.deltaTime;
        else if (isShiftHold)
            currentStamina += penaltyRegenRate * Time.deltaTime;
        else
            currentStamina += normalRegenRate * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        // ระบบแรงโน้มถ่วง
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y = Mathf.Max(velocity.y + gravity * Time.deltaTime, maxFallSpeed);
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleInteraction()
    {
        if (isTransactionActive) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        bool foundInteractable = false;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null && interactable.CanInteract())
            {
                foundInteractable = true;

                if (interactPromptUI != null) interactPromptUI.Show(interactable.GetPromptText());

                if (Input.GetKeyDown(KeyCode.E))
                {
                    isTransactionActive = true;
                    if (interactPromptUI != null) interactPromptUI.Hide();
                    interactable.Interact();
                }
            }
        }

        if (!foundInteractable && interactPromptUI != null && interactPromptUI.IsVisible)
            interactPromptUI.Hide();
    }

    public void ResetInteraction()
    {
        isTransactionActive = false;
    }
}