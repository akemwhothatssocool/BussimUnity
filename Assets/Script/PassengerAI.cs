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

    public bool CanInteract()
    {
        return currentState == State.WaitingForFare && !hasPaid && !isProcessingPayment;
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
        if (moneyProp != null) moneyProp.SetActive(false);

        Transform iconTransform = transform.Find("MoodIcon");
        if (iconTransform != null) moodIconRenderer = iconTransform.GetComponent<SpriteRenderer>();

        SetMood(Mood.None);

        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.autoBraking = true;
            agent.stoppingDistance = 0.3f;
        }
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
        float smoothSpeed = Mathf.SmoothDamp(currentSpeed, normalizedSpeed, ref speedVelocity, speedSmoothTime);
        animator.SetFloat("Speed", smoothSpeed);
    }

    // ✅ อัปเดตอารมณ์บนหัว (เพื่อความกดดันทางสายตาเท่านั้น)
    void UpdateMoodOverTime()
    {
        if (currentState == State.WaitingForFare || currentState == State.Paying || currentState == State.HandExtended)
        {
            currentWaitTime += Time.deltaTime;

            if (currentWaitTime >= timeToAngry) SetMood(Mood.Angry);
            else if (currentWaitTime >= timeToNeutral) SetMood(Mood.Neutral);
            else SetMood(Mood.Happy);
        }
    }

    // ✅ ระบบคิดเงินสายซอฟต์: เน้นให้รางวัลคนทอนไว!
    public void PaymentCompleted()
    {
        hasPaid = true;
        currentState = State.Riding;

        float popularityChange = 0f;

        // คำนวณโบนัสความนิยมตามความไว (ไม่มีการติดลบ)
        if (currentWaitTime < timeToNeutral)
        {
            popularityChange = 3f; // ⚡ ทอนไวมาก ได้โบนัสเยอะ
            Debug.Log("<color=green>ทอนไว! +3 Popularity</color>");
        }
        else
        {
            popularityChange = 1f; // 😐 ทอนช้า ก็ยังได้คะแนนความพยายาม
            Debug.Log("<color=white>ทอนเสร็จสิ้น +1 Popularity</color>");
        }

        // ส่งคะแนนไปที่ GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdjustPopularity(popularityChange);
        }

        SetMood(Mood.None);
        if (moneyProp != null) moneyProp.SetActive(false); // ซ่อนเงินในมือ
        StartCoroutine(RideAndGetOff());
    }

    // --- ส่วนที่เหลือของโค้ด (BoardBus, GoToSeat, etc.) เหมือนเดิม ---
    public void WaitAtStop(Transform waitPoint)
    {
        if (agent != null) agent.enabled = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        transform.position = waitPoint.position;
        transform.rotation = waitPoint.rotation;
        transform.SetParent(waitPoint);
    }

    public void BoardBus()
    {
        transform.SetParent(null);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        if (agent != null) agent.enabled = true;
        GoToSeat();
    }

    void GoToSeat()
    {
        currentState = State.FindingSeat;
        if (agent != null && mySeatPoint != null) StartCoroutine(WaitUntilSeated());
    }

    IEnumerator WaitUntilSeated()
    {
        yield return null;
        if (agent != null && !agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas)) agent.Warp(hit.position);
        }
        agent.isStopped = false;
        agent.SetDestination(mySeatPoint.position);

        while (agent.isOnNavMesh && (agent.pathPending || agent.remainingDistance > 0.5f)) yield return null;

        if (mySeatPoint != null) yield return StartCoroutine(SnapToSeat(mySeatPoint));

        if (isSittingSeat) animator.SetBool("isSitting", true);
        else animator.SetTrigger("trigStand");

        currentState = State.WaitingForFare;
        currentWaitTime = 0f;
    }

    IEnumerator SnapToSeat(Transform seatPoint)
    {
        if (agent != null) { agent.isStopped = true; agent.enabled = false; }
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
        if (fareSystem != null) fareSystem.StartTransaction(this);
        yield return new WaitForSeconds(1.0f);
        isProcessingPayment = false;
    }

    public Transform GetHandPosition()
    {
        if (!isSittingSeat) return handPosStand;
        return isRightSide ? handPosSitR : handPosSitL;
    }

    IEnumerator RideAndGetOff()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(10f, 20f));
        currentState = State.Exiting;
        if (cityManager != null) yield return new WaitUntil(() => Mathf.Abs(cityManager._currentSpeed) < 0.05f);
        if (isSittingSeat) { animator.SetBool("isSitting", false); yield return new WaitForSeconds(2f); }

        if (agent != null)
        {
            agent.enabled = true; yield return null;
            if (agent.isOnNavMesh && exitPoint != null)
            {
                agent.SetDestination(exitPoint.position);
                while (agent.isOnNavMesh && agent.remainingDistance > 1.0f) yield return null;
            }
        }
        onExitBus?.Invoke();
        Destroy(gameObject);
    }

    public void SetMood(Mood newMood)
    {
        if (moodIconRenderer == null) return;
        if (newMood == Mood.None) { moodIconRenderer.gameObject.SetActive(false); return; }
        moodIconRenderer.gameObject.SetActive(true);
        switch (newMood)
        {
            case Mood.Happy: moodIconRenderer.sprite = iconHappy; break;
            case Mood.Neutral: moodIconRenderer.sprite = iconNeutral; break;
            case Mood.Angry: moodIconRenderer.sprite = iconAngry; break;
        }
    }
}