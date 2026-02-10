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

    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 velocity;
    public float currentStamina;
    private float activeMoveSpeed;

    private bool isInteractingWithUI = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina;
        activeMoveSpeed = walkSpeed;

        if (interactTextUI != null) interactTextUI.SetActive(false);
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

        if (isShiftHold && isMoving && currentStamina > 0)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            if (isShiftHold) currentStamina += penaltyRegenRate * Time.deltaTime;
            else currentStamina += normalRegenRate * Time.deltaTime;
        }
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleInteraction()
    {
        Debug.DrawRay(playerCamera.position, playerCamera.forward * interactRange, Color.red);

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        bool foundTarget = false;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            PassengerAI passenger = hit.collider.GetComponent<PassengerAI>();

            if (passenger != null &&
                passenger.currentState == PassengerAI.State.WaitingForFare &&
                !passenger.hasPaid)
            {
                foundTarget = true;

                if (interactTextUI != null)
                {
                    interactTextUI.SetActive(true);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    passenger.Interact();
                }
            }
        }

        if (!foundTarget)
        {
            if (interactTextUI != null && interactTextUI.activeSelf)
            {
                interactTextUI.SetActive(false);
            }
        }
    }
}
