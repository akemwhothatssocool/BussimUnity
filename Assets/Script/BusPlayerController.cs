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

    [Header("4. ระบบเก็บเงิน (Interaction)")]
    public float interactRange = 2.0f;
    public LayerMask interactLayer;
    public GameObject interactTextUI;

    [Header("5. ระบบ Payment UI")]
    public GameObject uiPanel;
    public UnityEngine.UI.Text textPrice;
    public Animator npcAnimator;

    // --- ตัวแปรภายใน ---
    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 velocity;
    public float currentStamina;
    private float activeMoveSpeed;

    private bool isTransactionActive = false;

    private float moneyReceived = 0;
    private float currentChange = 0;
    private int currentPoseState = 0;
    private PassengerAI currentPassenger = null;

    private const float maxFallSpeed = -20f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina;
        activeMoveSpeed = walkSpeed;

        if (interactTextUI != null) interactTextUI.SetActive(false);

        if (interactLayer == 0)
            Debug.LogWarning("WARNING: interactLayer not set in Inspector! Raycast will not work.");
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
        {
            if (currentStamina > 0) targetSpeed = runSpeed;
            else targetSpeed = exhaustedSpeed;
        }
        else
        {
            targetSpeed = walkSpeed;
        }

        activeMoveSpeed = Mathf.Lerp(activeMoveSpeed, targetSpeed, acceleration * Time.deltaTime);
        controller.Move(direction * activeMoveSpeed * Time.deltaTime);

        // Stamina Logic
        if (isShiftHold && isMoving && currentStamina > 0)
            currentStamina -= staminaDrainRate * Time.deltaTime;
        else if (isShiftHold)
            currentStamina += penaltyRegenRate * Time.deltaTime;
        else
            currentStamina += normalRegenRate * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        // Gravity with clamp
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y = Mathf.Max(velocity.y + gravity * Time.deltaTime, maxFallSpeed);
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleInteraction()
    {
        if (isTransactionActive) return;

        Debug.DrawRay(playerCamera.position, playerCamera.forward * interactRange, Color.red);

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        bool foundTarget = false;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            PassengerAI passenger = hit.collider.GetComponent<PassengerAI>();

            if (passenger != null && passenger.currentState == PassengerAI.State.WaitingForFare && !passenger.hasPaid)
            {
                foundTarget = true;

                if (interactTextUI != null)
                    interactTextUI.SetActive(true);
                else
                    Debug.LogError("interactTextUI is NULL! Please assign in Inspector!");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    isTransactionActive = true;
                    if (interactTextUI != null) interactTextUI.SetActive(false);
                    passenger.Interact();
                }
            }
        }

        if (!foundTarget)
        {
            if (interactTextUI != null && interactTextUI.activeSelf)
                interactTextUI.SetActive(false);
        }
    }

    // Public method ให้ FareSystem เรียก reset state หลังปิด UI
    public void ResetInteraction()
    {
        isTransactionActive = false;
    }
}