using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class PassengerAI : MonoBehaviour
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

    public void SetSeat(Transform seatPoint)
    {
        mySeatPoint = seatPoint;

        if (seatPoint.name.Contains("Sit_R") || seatPoint.name.Contains("_R"))
            isRightSide = true;
        else
            isRightSide = false;

        Debug.Log($"🎯 {gameObject.name} SetSeat: {seatPoint.name}, Right side: {isRightSide}");
    }

    void Start()
    {
        fareSystem = FindObjectOfType<FareSystem>();
        if (fareSystem == null)
            Debug.LogError("❌ ไม่เจอ FareSystem ใน Scene!");

        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.autoBraking = true;
            agent.stoppingDistance = 0.3f;
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name} NavMesh Agent is NULL!");
        }

        Debug.Log($"🪑 {gameObject.name} mySeatPoint: {(mySeatPoint != null ? mySeatPoint.name : "NULL")}");
        Debug.Log($"🪑 {gameObject.name} isSittingSeat: {isSittingSeat}");

        GoToSeat();
    }

    void Update()
    {
        UpdateAnimationSpeed();
    }

    public void Interact()
    {
        if (hasPaid || isProcessingPayment || currentState != State.WaitingForFare) return;

        isProcessingPayment = true;
        StartCoroutine(PayRoutine());
    }

    void UpdateAnimationSpeed()
    {
        if (agent == null || animator == null) return;
        if (!agent.enabled) return;

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
        Debug.Log($"🚀 {gameObject.name} GoToSeat() called");
        currentState = State.FindingSeat;

        if (agent != null && mySeatPoint != null)
        {
            agent.isStopped = false;
            bool success = agent.SetDestination(mySeatPoint.position);
            Debug.Log($"🎯 {gameObject.name} SetDestination to {mySeatPoint.name}: {(success ? "SUCCESS" : "FAILED")}");
            StartCoroutine(WaitUntilSeated());
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name} Agent or MySeatPoint is None!");
        }
    }

    IEnumerator WaitUntilSeated()
    {
        Debug.Log($"⏰ {gameObject.name} WaitUntilSeated() START");

        float timeout = 10f;
        float elapsed = 0f;

        while (agent.pathPending)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed > timeout)
            {
                Debug.LogError($"❌ {gameObject.name} Timeout waiting for path!");
                yield break;
            }
        }

        animator.SetBool("isSitting", false);
        Debug.Log($"🚶 {gameObject.name} Walking to seat...");

        elapsed = 0f;

        while (agent.remainingDistance > 0.5f)
        {
            yield return null;
            elapsed += Time.deltaTime;

            if (elapsed > timeout)
            {
                Debug.LogWarning($"⚠️ {gameObject.name} Timeout!");
                break;
            }
        }

        Debug.Log($"🎯 {gameObject.name} Reached seat area");

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        yield return new WaitForSeconds(0.5f);

        if (mySeatPoint != null)
            StartCoroutine(RotateTowards(mySeatPoint.rotation));

        if (isSittingSeat)
        {
            Debug.Log($"💺 {gameObject.name} Set isSitting = true");
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
        Debug.Log($"✅ {gameObject.name} Ready for payment!");
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
        Debug.Log($"💰 {gameObject.name} Starting payment");
        currentState = State.Paying;

        if (fareSystem != null)
            fareSystem.StartTransaction(this);
        else
            Debug.LogError("❌ FareSystem เป็น null!");

        yield return new WaitForSeconds(1.0f);
        isProcessingPayment = false;
    }

    public Transform GetHandPosition()
    {
        Transform hand = null;

        if (!isSittingSeat)
            hand = handPosStand;
        else if (isRightSide)
            hand = handPosSitR;
        else
            hand = handPosSitL;

        if (hand != null)
            Debug.Log($"🖐️ {gameObject.name} GetHandPosition: {hand.name} at {hand.position}");
        else
            Debug.LogError($"❌ {gameObject.name} Hand Transform is NULL!");

        return hand;
    }

    public void PaymentCompleted()
    {
        Debug.Log($"✅ {gameObject.name} Payment completed");
        hasPaid = true;
        currentState = State.Riding;
        StartCoroutine(RideAndGetOff());
    }

    IEnumerator RideAndGetOff()
    {
        float rideTime = UnityEngine.Random.Range(10f, 20f);
        Debug.Log($"🕐 {gameObject.name} Riding for {rideTime:F1} seconds");
        yield return new WaitForSeconds(rideTime);

        Debug.Log($"🚪 {gameObject.name} Time to exit");
        currentState = State.Exiting;

        // ลุกจากที่นั่งก่อน
        if (isSittingSeat)
        {
            animator.SetBool("isSitting", false);
            yield return new WaitForSeconds(2.0f);

            if (agent != null)
            {
                agent.enabled = true;
                Debug.Log($"🔓 {gameObject.name} Agent enabled - ready to exit");
            }
        }

        if (agent != null && exitPoint != null)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPoint.position);
            Debug.Log($"🚶 {gameObject.name} Walking to exit at {exitPoint.position}");

            // ✅ FIX: รอให้ NavMesh คำนวณเส้นทางเสร็จก่อน (pathPending = true ตอนกำลังคำนวณ)
            float timeout = 5f;
            float elapsed = 0f;
            while (agent.pathPending)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (elapsed > timeout)
                {
                    Debug.LogWarning($"⚠️ {gameObject.name} Path calculation timeout!");
                    break;
                }
            }

            // ✅ FIX: จากนั้นค่อยรอจนถึง exitPoint
            elapsed = 0f;
            while (agent.remainingDistance > 1.0f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (elapsed > 30f) // timeout กันค้างนาน
                {
                    Debug.LogWarning($"⚠️ {gameObject.name} Exit walk timeout!");
                    break;
                }
            }
        }
        else
        {
            if (agent == null) Debug.LogError($"❌ {gameObject.name} Agent is NULL on exit!");
            if (exitPoint == null) Debug.LogError($"❌ {gameObject.name} exitPoint is NULL! ตรวจสอบ BusStopManager ว่า assign exitPoint แล้วหรือยัง");
        }

        onExitBus?.Invoke();
        Debug.Log($"👋 {gameObject.name} Exited the bus");

        Destroy(gameObject);
    }
}