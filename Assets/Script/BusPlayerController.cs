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
    public CityManager cityManager; // ✅ ประกาศแค่ครั้งเดียว
    public bool isInsideBus = true;
    public Vector3 scrollDirection = new Vector3(-1, 0, 0);

    // Private variables
    private CharacterController controller; // ✅ ใช้ตัวเดียว ลบ cc ออก
    private float lastBusSpeed;
    private Vector3 inertiaVelocity;
    private float xRotation = 0f;
    private Vector3 velocity;
    public float currentStamina;
    private float activeMoveSpeed;
    private bool isTransactionActive = false;
    private const float maxFallSpeed = -20f;

    void Start()
    {
        controller = GetComponent<CharacterController>(); // ✅ ดึงครั้งเดียว
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

        // 🌟 ถอยหลังตามฉาก ถ้าไม่ได้อยู่บนรถ!
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

        float targetSpeed;
        if (isShiftHold && isMoving)
            targetSpeed = (currentStamina > 0) ? runSpeed : exhaustedSpeed;
        else
            targetSpeed = walkSpeed;

        activeMoveSpeed = Mathf.Lerp(activeMoveSpeed, targetSpeed, acceleration * Time.deltaTime);

        // คำนวณแรงเฉื่อย
        if (cityManager != null)
        {
            float currentBusSpeed = cityManager._currentSpeed;
            float deltaSpeed = currentBusSpeed - lastBusSpeed;

            if (Mathf.Abs(deltaSpeed) < 2f && Mathf.Abs(deltaSpeed) > 0.001f)
            {
                inertiaVelocity += Vector3.left * (deltaSpeed * inertiaMultiplier);
            }
            lastBusSpeed = currentBusSpeed;
        }

        inertiaVelocity = Vector3.ClampMagnitude(inertiaVelocity, 5f);

        // คำนวณการเอียงตัว
        if (cityManager != null)
        {
            float leanAmount = inertiaVelocity.x * leanSensitivity;
            leanAmount = Mathf.Clamp(leanAmount, -maxLeanAngle, maxLeanAngle);

            float currentYRotation = transform.localEulerAngles.y;
            Quaternion targetLeanRotation = Quaternion.Euler(leanAmount, currentYRotation, 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLeanRotation, 7f * Time.deltaTime);
        }

        inertiaVelocity = Vector3.Lerp(inertiaVelocity, Vector3.zero, stumbleRecoverySpeed * Time.deltaTime);

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

    // ✅ ระบบเช็คว่าอยู่บนรถหรือเปล่า
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("BusZone"))
        {
            isInsideBus = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("BusZone"))
        {
            isInsideBus = false;
        }
    }
}