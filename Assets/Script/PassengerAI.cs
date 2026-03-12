using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class PassengerAI : MonoBehaviour, IInteractable
{
    public enum State { Boarding, FindingSeat, Seated, WaitingForFare, HandExtended, Paying, Riding, Exiting }
    public State currentState = State.Boarding;

    [Header("การตั้งค่า")]
    public bool hasPaid = false;

    public enum Mood { None, Happy, Neutral, Angry }

    [Header("ระบบอารมณ์บนหัว")]
    private SpriteRenderer moodIconRenderer;
    public Sprite iconHappy;
    public Sprite iconNeutral;
    public Sprite iconAngry;

    [Header("ตั้งค่าความอดทน (วินาที)")]
    public float timeToNeutral = 10f;
    public float timeToAngry = 20f;
    private float currentWaitTime = 0f;

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

    [Header("Props")]
    public GameObject moneyProp;

    [Header("ตำแหน่ง (Assign จาก Spawner)")]
    public Transform mySeatPoint;
    public Transform exitPoint;
    public bool isSittingSeat = false;
    public bool isRightSide = false;

    [Header("ระบบรถ")]
    public CityManager cityManager;

    [Header("Events")]
    public Action onExitBus;

    private FareSystem fareSystem;
    private bool isProcessingPayment = false;

    Rigidbody rb;

    public bool CanInteract()
    {
        return currentState == State.WaitingForFare
               && !hasPaid
               && !isProcessingPayment;
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        currentState = State.HandExtended;

        if (animator != null)
        {
            if (isRightSide) animator.SetTrigger("trigSitGiveR");
            else animator.SetTrigger("trigSitGiveL");
        }

        if (moneyProp != null) moneyProp.SetActive(true);

        isProcessingPayment = true;
        StartCoroutine(PayRoutine());
    }

    public string GetPromptText()
    {
        if (currentState == State.WaitingForFare) return "กด E เพื่อรับเงิน";
        if (currentState == State.HandExtended) return "คลิกเพื่อรับเงิน";
        return "";
    }

    public void SetSeat(Transform seatPoint)
    {
        mySeatPoint = seatPoint;
        isRightSide = seatPoint.name.Contains("Sit_R") || seatPoint.name.Contains("_R");
    }

    void Start()
    {
        fareSystem = FindFirstObjectByType<FareSystem>();
        rb = GetComponent<Rigidbody>();

        if (moneyProp != null) moneyProp.SetActive(false);

        Transform iconTransform = transform.Find("MoodIcon");

        if (iconTransform != null)
            moodIconRenderer = iconTransform.GetComponent<SpriteRenderer>();
        else
            Debug.LogWarning($"{gameObject.name} หา MoodIcon ไม่เจอ!");

        SetMood(Mood.None);

        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.autoBraking = true;
            agent.stoppingDistance = 0.3f;
        }

        // ❌ ไม่เรียก GoToSeat แล้ว
    }

    void Update()
    {
        UpdateAnimationSpeed();
        UpdateMoodOverTime();
    }

    void UpdateAnimationSpeed()
    {
        if (agent == null || animator == null || !agent.enabled) return;

        Vector3 velocity = agent.velocity;
        velocity.y = 0;

        float speed = velocity.magnitude;
        float normalizedSpeed = speed / agent.speed;

        float currentSpeed = animator.GetFloat("Speed");

        float smoothSpeed = Mathf.SmoothDamp(
            currentSpeed,
            normalizedSpeed,
            ref speedVelocity,
            speedSmoothTime
        );

        animator.SetFloat("Speed", smoothSpeed);
    }

    void UpdateMoodOverTime()
    {
        if (currentState == State.WaitingForFare)
        {
            currentWaitTime += Time.deltaTime;

            if (currentWaitTime >= timeToAngry)
                SetMood(Mood.Angry);
            else if (currentWaitTime >= timeToNeutral)
                SetMood(Mood.Neutral);
        }
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
    }

    IEnumerator WaitUntilSeated()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (agent.pathPending)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed > timeout) break;
        }

        animator.SetBool("isSitting", false);

        elapsed = 0f;

        while (agent.remainingDistance > 0.5f)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed > timeout) break;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (mySeatPoint != null)
            yield return StartCoroutine(SnapToSeat(mySeatPoint));

        if (isSittingSeat)
        {
            animator.SetBool("isSitting", true);
            yield return new WaitForSeconds(2.5f);
        }
        else
        {
            animator.SetBool("isSitting", false);
            animator.SetTrigger("trigStand");
        }

        currentState = State.WaitingForFare;
        currentWaitTime = 0f;

        SetMood(Mood.Happy);
    }

    IEnumerator SnapToSeat(Transform seatPoint)
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (animator != null) animator.SetFloat("Speed", 0f);

        if (agent != null) agent.enabled = false;

        float duration = 0.6f;
        float elapsed = 0f;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, seatPoint.position, t);
            transform.rotation = Quaternion.Slerp(startRot, seatPoint.rotation, t);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.position = seatPoint.position;
        transform.rotation = seatPoint.rotation;
    }

    IEnumerator PayRoutine()
    {
        currentState = State.Paying;

        if (fareSystem != null)
            fareSystem.StartTransaction(this);

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
        {
            Collider handCollider = hand.GetComponent<Collider>();

            if (handCollider != null)
                handCollider.enabled = false;
        }

        return hand;
    }

    public void PaymentCompleted()
    {
        hasPaid = true;

        currentState = State.Riding;

        SetMood(Mood.None);

        StartCoroutine(RideAndGetOff());
    }

    IEnumerator RideAndGetOff()
    {
        float rideTime = UnityEngine.Random.Range(10f, 20f);

        yield return new WaitForSeconds(rideTime);

        SetMood(Mood.None);

        currentState = State.Exiting;

        if (cityManager != null)
            yield return new WaitUntil(() => Mathf.Abs(cityManager._currentSpeed) < 0.05f);

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

            float timeout = 5f;
            float elapsed = 0f;

            while (agent.pathPending)
            {
                yield return null;

                elapsed += Time.deltaTime;

                if (elapsed > timeout) break;
            }

            elapsed = 0f;

            while (agent.remainingDistance > 1.0f)
            {
                yield return null;

                elapsed += Time.deltaTime;

                if (elapsed > 30f) break;
            }
        }

        onExitBus?.Invoke();

        Destroy(gameObject);
    }

    public void SetMood(Mood newMood)
    {
        if (moodIconRenderer == null) return;

        if (newMood == Mood.None)
        {
            moodIconRenderer.gameObject.SetActive(false);
            return;
        }

        moodIconRenderer.gameObject.SetActive(true);

        switch (newMood)
        {
            case Mood.Happy: moodIconRenderer.sprite = iconHappy; break;
            case Mood.Neutral: moodIconRenderer.sprite = iconNeutral; break;
            case Mood.Angry: moodIconRenderer.sprite = iconAngry; break;
        }
    }

    // ===============================
    // ระบบรอป้ายรถ
    // ===============================

    public void WaitAtStop(Transform waitPoint)
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = waitPoint.position;
        transform.rotation = waitPoint.rotation;

        transform.SetParent(waitPoint);
    }

    public void BoardBus()
    {
        transform.SetParent(null);

        if (rb != null)
            rb.isKinematic = false;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        GoToSeat();
    }
}