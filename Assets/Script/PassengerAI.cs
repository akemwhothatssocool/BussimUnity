using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class PassengerAI : MonoBehaviour, IInteractable
{
    public enum State { Boarding, FindingSeat, Seated, WaitingForFare, HandExtended, Paying, Riding, Exiting }
    public State currentState = State.Boarding;
    public int targetStop;

    [Header("การตั้งค่าอารมณ์")]
    public bool hasPaid = false;
    public enum Mood { None, Happy, Neutral, Angry }
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

    [Header("Component")]
    public NavMeshAgent agent;
    public Animator animator;
    private Rigidbody rb;

    [Header("ตำแหน่งมือ/อุปกรณ์")]
    public Transform handPosStand;
    public Transform handPosSitL;
    public Transform handPosSitR;
    public GameObject moneyProp;

    [Header("ตำแหน่ง (Assign จาก Spawner)")]
    public Transform mySeatPoint;
    public Transform exitPoint;
    public bool isSittingSeat = false;
    public bool isRightSide = false;

    [Header("ระบบรถ/เงิน")]
    public CityManager cityManager;
    private FareSystem fareSystem;

    // 🟢 เพิ่มใหม่: หมวดหมู่ระบบตัวเหม็น
    [Header("ระบบตัวเหม็น (Toxic)")]
    public bool isToxic = false;      // ติ๊กเพื่อกำหนดให้ NPC ตัวนี้เหม็นตั้งแต่เกิด (หรือจะสุ่มจาก Spawner ก็ได้)
    public GameObject toxicVFX;       // ลากออบเจกต์ควันพิษ (Toxic / ToxicSmoke) มาใส่ช่องนี้

    [Header("Events")]
    public Action onExitBus;

    private bool isProcessingPayment = false;

    void Start()
    {
        fareSystem = FindFirstObjectByType<FareSystem>();
        rb = GetComponent<Rigidbody>();

        if (moneyProp != null) moneyProp.SetActive(false);

        // หา MoodIcon
        Transform iconTransform = transform.Find("MoodIcon");
        if (iconTransform != null) moodIconRenderer = iconTransform.GetComponent<SpriteRenderer>();

        SetMood(Mood.None);

        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.stoppingDistance = 0.3f;
        }

        // 🟢 เพิ่มใหม่: เช็กสถานะตัวเหม็นตอนเริ่มเกม แล้วเปิด/ปิดควันตามค่า isToxic
        if (toxicVFX != null)
        {
            toxicVFX.SetActive(isToxic);
        }
    }

    void Update()
    {
        UpdateAnimationSpeed();
        UpdateMoodOverTime();
    }

    // 🟢 เพิ่มใหม่: ฟังก์ชันสำหรับสั่งเปิด/ปิดความเหม็นระหว่างเล่นเกม
    public void SetToxicState(bool state)
    {
        isToxic = state;
        if (toxicVFX != null)
        {
            toxicVFX.SetActive(isToxic);
        }
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

    // ===============================
    // 🌟 ระบบขึ้นรถและหาที่นั่ง
    // ===============================

    public void WaitAtStop(Transform waitPoint)
    {
        if (agent != null) { agent.isStopped = true; agent.enabled = false; }
        if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }

        transform.position = waitPoint.position;
        transform.rotation = waitPoint.rotation;
        transform.SetParent(waitPoint);
    }

    public void BoardBus()
    {
        transform.SetParent(null);
        if (rb != null) rb.isKinematic = false;
        if (agent != null) { agent.enabled = true; agent.isStopped = false; }

        GoToSeat();
    }

    void GoToSeat()
    {
        currentState = State.FindingSeat;
        if (agent != null && mySeatPoint != null)
        {
            agent.SetDestination(mySeatPoint.position);
            StartCoroutine(WaitUntilSeated());
        }
    }

    IEnumerator WaitUntilSeated()
    {
        yield return null;

        while (agent.enabled && agent.isOnNavMesh && (agent.pathPending || agent.remainingDistance > 0.5f))
        {
            yield return null;
        }

        if (mySeatPoint != null) yield return StartCoroutine(SnapToSeat(mySeatPoint));

        if (isSittingSeat)
        {
            animator.SetBool("isSitting", true);
        }
        else
        {
            animator.SetBool("isSitting", false);
            animator.SetTrigger("trigStand");
        }

        currentState = State.WaitingForFare;
        currentWaitTime = 0f;
    }

    IEnumerator SnapToSeat(Transform seatPoint)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        if (agent != null) agent.enabled = false;
        if (animator != null) animator.SetFloat("Speed", 0f);

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

    // ===============================
    // 🌟 ระบบคิดเงิน
    // ===============================

    public bool CanInteract() { return currentState == State.WaitingForFare && !hasPaid && !isProcessingPayment; }

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

    IEnumerator PayRoutine()
    {
        currentState = State.Paying;
        if (fareSystem != null) fareSystem.StartTransaction(this);
        yield return new WaitForSeconds(1.0f);
        isProcessingPayment = false;
    }

    public void PaymentCompleted()
    {
        hasPaid = true;
        currentState = State.Riding;

        float popularityChange = (currentWaitTime < timeToNeutral) ? 3f : 1f;
        if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(popularityChange);

        SetMood(Mood.None);
        if (moneyProp != null) moneyProp.SetActive(false);
        StartCoroutine(RideAndGetOff());
    }

    // ===============================
    // 🌟 ระบบลงรถ
    // ===============================

    IEnumerator RideAndGetOff()
    {
        yield return new WaitUntil(() => currentState == State.Exiting);

        if (isSittingSeat)
        {
            animator.SetBool("isSitting", false);
            yield return new WaitForSeconds(1.5f);
        }

        if (agent != null)
        {
            agent.enabled = true;
            yield return null;

            if (agent.isOnNavMesh && exitPoint != null)
            {
                agent.isStopped = false;
                agent.SetDestination(exitPoint.position);

                yield return new WaitUntil(() => !agent.pathPending);

                float timeout = 5f;
                while (agent.remainingDistance > 0.5f && timeout > 0)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        Debug.Log($"{gameObject.name} ลงรถเรียบร้อยที่ป้ายตามเป้าหมายจ้า");
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

    public Transform GetHandPosition()
    {
        if (!isSittingSeat) return handPosStand;
        return isRightSide ? handPosSitR : handPosSitL;
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
}