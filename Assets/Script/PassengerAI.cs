using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class PassengerAI : MonoBehaviour, IInteractable
{
    public enum State { Boarding, FindingSeat, Seated, WaitingForFare, Paying, Riding, Exiting }
    public State currentState = State.Boarding;

    [Header("การตั้งค่า")]
    public bool hasPaid = false;

    [Header("Animation Settings")]
    public float speedSmoothTime = 0.1f;
    private float speedVelocity;

    [Header("Component ที่ต้องใส่")]
    public NavMeshAgent agent;
    public Animator animator;

    [Header("ตำแหน่งมือ")]
    public Transform handPosStand;
    public Transform handPosSitL;
    public Transform handPosSitR;

    [Header("ตำแหน่ง (Assign จาก Spawner)")]
    public Transform mySeatPoint;
    public Transform exitPoint;
    public bool isSittingSeat = false;
    public bool isRightSide = false;

    [Header("Events")]
    public Action onExitBus;

    private FareSystem fareSystem;
    private bool isProcessingPayment = false;

    // ============================================================
    // IInteractable Implementation
    // ============================================================
    public bool CanInteract()
    {
        return currentState == State.WaitingForFare
               && !hasPaid
               && !isProcessingPayment;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        isProcessingPayment = true;
        StartCoroutine(PayRoutine());
    }

    public string GetPromptText()
    {
        return "กด E เพื่อเก็บค่าโดยสาร";
    }
    // ============================================================

    public void SetSeat(Transform seatPoint)
    {
        mySeatPoint = seatPoint;
        isRightSide = seatPoint.name.Contains("Sit_R") || seatPoint.name.Contains("_R");
        Debug.Log($"SetSeat: {seatPoint.name}, Right side: {isRightSide}");
    }

    void Start()
    {
        // ✅ FIX W1: เปลี่ยนจาก FindObjectOfType (obsolete) เป็น FindFirstObjectByType
        fareSystem = FindFirstObjectByType<FareSystem>();
        if (fareSystem == null) Debug.LogError("ไม่เจอ FareSystem ใน Scene!");

        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.autoBraking = true;
            agent.stoppingDistance = 0.3f;
        }
        else Debug.LogError($"{gameObject.name} NavMesh Agent is NULL!");

        GoToSeat();
    }

    void Update() { UpdateAnimationSpeed(); }

    void UpdateAnimationSpeed()
    {
        if (agent == null || animator == null || !agent.enabled) return;

        Vector3 velocity = agent.velocity;
        velocity.y = 0;
        float speed = velocity.magnitude;
        float normalizedSpeed = speed / agent.speed;

        float currentSpeed = animator.GetFloat("Speed");
        float smoothSpeed = Mathf.SmoothDamp(currentSpeed, normalizedSpeed, ref speedVelocity, speedSmoothTime);
        animator.SetFloat("Speed", smoothSpeed);
    }

    void GoToSeat()
    {
        currentState = State.FindingSeat;
        if (agent != null && mySeatPoint != null)
        {
            agent.isStopped = false;
            agent.SetDestination(mySeatPoint.position);
            StartCoroutine(WaitUntilSeated());
        }
        else Debug.LogError($"{gameObject.name} Agent or MySeatPoint is None!");
    }

    IEnumerator WaitUntilSeated()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (agent.pathPending)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed > timeout) { Debug.LogError($"{gameObject.name} Timeout waiting for path!"); yield break; }
        }

        animator.SetBool("isSitting", false);
        elapsed = 0f;

        while (agent.remainingDistance > 0.5f)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed > timeout) { Debug.LogWarning($"{gameObject.name} Timeout!"); break; }
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        yield return new WaitForSeconds(0.5f);

        if (mySeatPoint != null)
            StartCoroutine(RotateTowards(mySeatPoint.rotation));

        if (isSittingSeat)
        {
            animator.SetBool("isSitting", true);
            yield return new WaitForSeconds(2.5f);
            if (agent != null) agent.enabled = false;
        }
        else
        {
            animator.SetBool("isSitting", false);
            animator.SetTrigger("trigStand");
        }

        currentState = State.WaitingForFare;
        Debug.Log($"{gameObject.name} Ready for payment!");
    }

    IEnumerator RotateTowards(Quaternion targetRotation)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;

        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    IEnumerator PayRoutine()
    {
        Debug.Log($"{gameObject.name} Starting payment");
        currentState = State.Paying;

        if (fareSystem != null)
            fareSystem.StartTransaction(this);
        else
            Debug.LogError("FareSystem เป็น null!");

        yield return new WaitForSeconds(1.0f);
        isProcessingPayment = false;
    }

    public Transform GetHandPosition()
    {
        Transform hand = null;
        if (!isSittingSeat) hand = handPosStand;
        else if (isRightSide) hand = handPosSitR;
        else hand = handPosSitL;

        if (hand == null)
            Debug.LogError($"{gameObject.name} Hand Transform is NULL!");
        return hand;
    }

    public void PaymentCompleted()
    {
        Debug.Log($"{gameObject.name} Payment completed");
        hasPaid = true;
        currentState = State.Riding;
        StartCoroutine(RideAndGetOff());
    }

    IEnumerator RideAndGetOff()
    {
        float rideTime = UnityEngine.Random.Range(10f, 20f);
        yield return new WaitForSeconds(rideTime);

        currentState = State.Exiting;

        if (isSittingSeat)
        {
            animator.SetBool("isSitting", false);
            yield return new WaitForSeconds(2.0f);
            if (agent != null) agent.enabled = true;
        }

        if (agent != null && exitPoint != null)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPoint.position);
            Debug.Log($"{gameObject.name} Walking to exit");

            float timeout = 5f;
            float elapsed = 0f;
            while (agent.pathPending)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (elapsed > timeout) { Debug.LogWarning("Path timeout!"); break; }
            }

            elapsed = 0f;
            while (agent.remainingDistance > 1.0f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (elapsed > 30f) { Debug.LogWarning("Exit walk timeout!"); break; }
            }
        }
        else
        {
            if (exitPoint == null) Debug.LogError($"{gameObject.name} exitPoint is NULL!");
        }

        onExitBus?.Invoke();
        Debug.Log($"{gameObject.name} Exited the bus");
        Destroy(gameObject);
    }
}