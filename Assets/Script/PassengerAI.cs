using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class PassengerAI : MonoBehaviour, IInteractable
{
    const string ToxicVfxObjectName = "ToxicSmoke";

    public enum State { Boarding, FindingSeat, Seated, WaitingForFare, HandExtended, Paying, Riding, Exiting }
    public enum RandomEventType { None, ToxicSmell, DrunkDance, LoudPhone }
    public State currentState = State.Boarding;
    public int targetStop;

    public enum Mood { None, Happy, Neutral, Angry }

    // ✅ ย้าย Header มาอยู่บน field แทน
    [Header("การตั้งค่าอารมณ์")]
    private SpriteRenderer moodIconRenderer;
    public Sprite iconHappy;
    public Sprite iconNeutral;
    public Sprite iconAngry;

    // ✅ ประกาศตัวแปรครั้งเดียว — ใช้ตัวนี้ทั้งระบบ
    // hasPaid       = ผู้เล่นเริ่ม interact แล้ว (FareSystem เริ่ม transaction)
    // hasPaidTicket = ปิด transaction เสร็จสมบูรณ์ (FareSystem.CloseTransaction เรียก setter นี้)
    [HideInInspector] public bool hasPaid = false;

    private bool _hasPaidTicket = false;
    public bool hasPaidTicket
    {
        get => _hasPaidTicket;
        set
        {
            _hasPaidTicket = value;
            // ซิงค์ hasPaid ไปด้วยเสมอ เพื่อป้องกัน logic แตก
            if (value) hasPaid = true;
        }
    }

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
    private BusSeat assignedSeat;

    [Header("ระบบรถ/เงิน")]
    public CityManager cityManager;
    private FareSystem fareSystem;

    [Header("ระบบตัวเหม็น (Toxic)")]
    public bool isToxic = false;
    public GameObject toxicVFX;
    [SerializeField] GameObject toxicVFXPrefab;
    [SerializeField] Vector3 toxicVFXLocalOffset = new Vector3(0f, 0.18f, 0.14f);

    [Header("Events")]
    public Action onExitBus;
    public Action<PassengerAI, bool> onRandomEventFinished;

    [Header("Random Events")]
    [SerializeField] RandomEventType activeRandomEvent = RandomEventType.None;
    [SerializeField] float drunkDanceYawAmplitude = 15f;
    [SerializeField] float drunkDanceBobAmplitude = 0.035f;
    [SerializeField] float loudPhoneTiltAmplitude = 9f;
    [SerializeField] float eventAnimationSpeed = 4.2f;

    [Header("Random Event Audio")]
    public AudioSource randomEventAudioSource;
    public AudioClip drunkDanceAudioClip;
    public AudioClip loudPhoneAudioClip;
    [Range(0f, 1f)] public float randomEventAudioVolume = 0.85f;
    public bool loopRandomEventAudio = true;

    [Header("Random Event Animation")]
    public Animator randomEventAnimator;
    public string drunkDanceStateName = "Base Layer.Dancing1";
    public string drunkDanceBoolName = "isDrunkDancing";
    public string drunkDanceTriggerName = "trigDrunkDance";
    public string drunkDanceStopTriggerName = "trigStopDrunkDance";
    public string standingRecoveryStateName = "Base Layer.Idle";
    public string sittingRecoveryStateName = "Base Layer.Sitting Idle";
    public string loudPhoneBoolName = "isTalkingLoudPhone";
    public string loudPhoneTriggerName = "trigLoudPhone";
    public string loudPhoneStopTriggerName = "trigStopLoudPhone";
    [SerializeField] float randomEventStateCrossFadeDuration = 0.15f;

    private bool isProcessingPayment = false;
    private Vector3 seatedBasePosition;
    private Quaternion seatedBaseRotation;
    private bool hasSeatedBasePose = false;
    private bool isUsingDirectDrunkDanceState = false;
    private GameObject runtimeToxicVFX;

    // ===============================
    void Start()
    {
        fareSystem = FindFirstObjectByType<FareSystem>();
        rb = GetComponent<Rigidbody>();

        if (randomEventAnimator == null)
            randomEventAnimator = animator;

        if (moneyProp != null) moneyProp.SetActive(false);

        Transform iconTransform = transform.Find("MoodIcon");
        if (iconTransform != null) moodIconRenderer = iconTransform.GetComponent<SpriteRenderer>();

        SetMood(Mood.None);

        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.stoppingDistance = 0.3f;
        }

        ResolveToxicVfxReference();
        ApplyToxicVfxState();

        EnsureRandomEventAudioSource();
    }

    void Update()
    {
        UpdateAnimationSpeed();
        UpdateMoodOverTime();
        UpdateRandomEventMotion();
        RecoverStalledPaymentState();
    }

    // ===============================
    // 🌟 ระบบ Toxic
    // ===============================

    public void SetToxicState(bool state)
    {
        isToxic = state;
        ApplyToxicVfxState();
    }

    void ResolveToxicVfxReference()
    {
        if (toxicVFX != null)
            return;

        Transform toxicTransform = FindChildRecursive(transform, ToxicVfxObjectName);
        if (toxicTransform != null)
            toxicVFX = toxicTransform.gameObject;
    }

    void ApplyToxicVfxState()
    {
        ResolveToxicVfxReference();
        GameObject activeVfx = GetToxicVfxTarget(isToxic);

        // Keep the old child object as an anchor only when a layered VFX prefab is assigned.
        if (toxicVFXPrefab != null && toxicVFX != null)
            SetToxicVfxObjectState(toxicVFX, false);

        if (isToxic)
        {
            if (activeVfx != null)
                SetToxicVfxObjectState(activeVfx, true);

            return;
        }

        if (activeVfx != null)
            SetToxicVfxObjectState(activeVfx, false);
        else if (toxicVFX != null)
            SetToxicVfxObjectState(toxicVFX, false);
    }

    GameObject GetToxicVfxTarget(bool createRuntimeInstance)
    {
        if (toxicVFXPrefab == null)
            return toxicVFX;

        if (runtimeToxicVFX == null && createRuntimeInstance)
            runtimeToxicVFX = CreateRuntimeToxicVfxInstance();

        return runtimeToxicVFX;
    }

    GameObject CreateRuntimeToxicVfxInstance()
    {
        Transform parent = transform;
        int siblingIndex = -1;
        Vector3 anchorLocalPosition = Vector3.zero;
        Quaternion anchorLocalRotation = Quaternion.identity;
        Vector3 anchorLocalScale = Vector3.one;

        if (toxicVFX != null)
        {
            Transform anchor = toxicVFX.transform;
            parent = anchor.parent != null ? anchor.parent : transform;
            siblingIndex = anchor.GetSiblingIndex();
            anchorLocalPosition = anchor.localPosition;
            anchorLocalRotation = anchor.localRotation;
            anchorLocalScale = anchor.localScale;
        }

        GameObject instance = Instantiate(toxicVFXPrefab, parent);
        instance.name = ToxicVfxObjectName + "_Runtime";

        Transform instanceTransform = instance.transform;
        Vector3 prefabLocalPosition = instanceTransform.localPosition;
        Quaternion prefabLocalRotation = instanceTransform.localRotation;
        Vector3 prefabLocalScale = instanceTransform.localScale;

        instanceTransform.localPosition = anchorLocalPosition + (anchorLocalRotation * toxicVFXLocalOffset) + prefabLocalPosition;
        instanceTransform.localRotation = anchorLocalRotation * prefabLocalRotation;
        instanceTransform.localScale = Vector3.Scale(anchorLocalScale, prefabLocalScale);

        if (siblingIndex >= 0)
            instanceTransform.SetSiblingIndex(Mathf.Min(siblingIndex, parent.childCount - 1));

        SetToxicVfxObjectState(instance, false);
        return instance;
    }

    void SetToxicVfxObjectState(GameObject vfxObject, bool shouldBeActive)
    {
        if (vfxObject == null)
            return;

        ParticleSystem[] particleSystems = vfxObject.GetComponentsInChildren<ParticleSystem>(true);
        if (shouldBeActive)
        {
            if (!vfxObject.activeSelf)
                vfxObject.SetActive(true);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null)
                    particleSystems[i].Play(true);
            }

            return;
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (vfxObject.activeSelf)
            vfxObject.SetActive(false);
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nestedChild = FindChildRecursive(child, childName);
            if (nestedChild != null)
                return nestedChild;
        }

        return null;
    }

    // ===============================
    // 🌟 Animation
    // ===============================

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
        if (currentState == State.WaitingForFare ||
            currentState == State.Paying ||
            currentState == State.HandExtended)
        {
            currentWaitTime += Time.deltaTime * GetPatienceDecayMultiplier();
            if (currentWaitTime >= timeToAngry)      SetMood(Mood.Angry);
            else if (currentWaitTime >= timeToNeutral) SetMood(Mood.Neutral);
            else                                       SetMood(Mood.Happy);
        }
    }

    // ===============================
    // 🌟 ระบบขึ้นรถและหาที่นั่ง
    // ===============================

    public void WaitAtStop(Vector3 waitPosition, Quaternion waitRotation, Transform waitParent = null)
    {
        // ✅ เช็ค isOnNavMesh ก่อนสั่ง isStopped เพื่อป้องกัน error
        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }
        if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }

        transform.position = waitPosition;
        transform.rotation = waitRotation;
        transform.SetParent(waitParent, true);
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

        while (agent.enabled && agent.isOnNavMesh &&
               (agent.pathPending || agent.remainingDistance > 0.5f))
        {
            yield return null;
        }

        if (mySeatPoint != null)
            yield return StartCoroutine(SnapToSeat(mySeatPoint));

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
        CaptureSeatPose();

        // ✅ แก้บั๊ก 1: เริ่ม coroutine ลงรถให้ทุกคน ไม่ว่าจะจ่ายเงินหรือไม่ก็ตาม
        StartCoroutine(RideAndGetOff());
    }

    IEnumerator SnapToSeat(Transform seatPoint)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.isStopped = true;

        if (agent != null) agent.enabled = false;
        if (animator != null) animator.SetFloat("Speed", 0f);

        float duration = 0.6f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Pose targetPose = assignedSeat != null
            ? assignedSeat.GetPassengerSnapPose(seatPoint)
            : new Pose(seatPoint.position, seatPoint.rotation);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPose.position, t);
            transform.rotation = Quaternion.Slerp(startRot, targetPose.rotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPose.position;
        transform.rotation = targetPose.rotation;
    }

    // ===============================
    // 🌟 ระบบคิดเงิน
    // ===============================

    public bool CanInteract()
    {
        if (CanResolveRandomEvent())
            return true;

        return currentState == State.WaitingForFare && !hasPaid && !isProcessingPayment;
    }

    public void Interact()
    {
        if (CanResolveRandomEvent())
        {
            ResolveRandomEvent();
            return;
        }

        if (!CanInteract()) return;
        currentState = State.HandExtended;

        if (animator != null)
        {
            if (isRightSide) animator.SetTrigger("trigSitGiveR");
            else             animator.SetTrigger("trigSitGiveL");
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
        RecoverStalledPaymentState();
    }

    /// <summary>
    /// เรียกจาก FareSystem.CloseTransaction() เมื่อรับเงินครบและทอนเสร็จแล้ว
    /// </summary>
    public void PaymentCompleted()
    {
        // ✅ ตั้งค่าทั้งสองตัวพร้อมกัน — ไม่มีทางหลุดอีกแล้ว
        hasPaidTicket = true; // setter จะตั้ง hasPaid = true ให้อัตโนมัติ

        currentState = State.Riding;
        CaptureSeatPose();

        float popularityChange = (currentWaitTime < timeToNeutral) ? 3f : 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.AdjustPopularity(popularityChange);

        SetMood(Mood.None);
        if (moneyProp != null) moneyProp.SetActive(false);

        // ✅ ไม่ต้อง StartCoroutine ใหม่ — RideAndGetOff() เริ่มไว้แล้วตั้งแต่นั่งลงที่นั่ง
    }

    // ===============================
    // 🌟 ระบบลงรถ + เช็กหนีตั๋ว
    // ===============================

    IEnumerator RideAndGetOff()
    {
        // รอจนกว่า BusController/StopManager จะเรียก TriggerExit() เพื่อเปลี่ยน State เป็น Exiting
        yield return new WaitUntil(() => currentState == State.Exiting);

        FinishRandomEvent(false);
        OnReachDestinationAndGetOff();

        // ลุกจากที่นั่ง
        if (isSittingSeat)
        {
            animator.SetBool("isSitting", false);
            yield return new WaitForSeconds(1.5f);
        }

        // เดินไปจุดลง
        if (agent != null)
        {
            agent.enabled = true;
            yield return null;

            if (!RestoreAgentToNavMesh())
            {
                Debug.LogWarning($"{gameObject.name}: Could not restore passenger to NavMesh before exiting.");
            }

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

        onExitBus?.Invoke();
        onExitBus = null;
        Destroy(gameObject);
    }

    bool RestoreAgentToNavMesh()
    {
        if (agent == null || !agent.enabled)
            return false;

        Vector3[] candidates = mySeatPoint != null
            ? new[] { transform.position, mySeatPoint.position }
            : new[] { transform.position };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (NavMesh.SamplePosition(candidates[i], out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                return agent.Warp(hit.position);
            }
        }

        return false;
    }

    /// <summary>
    /// เรียกจาก RideAndGetOff() ทันทีที่ถึงป้ายเป้าหมาย
    /// ตรวจสอบว่าจ่ายตั๋วหรือเปล่า แล้วจัดการ penalty / reward
    /// </summary>
    private void OnReachDestinationAndGetOff()
    {
        if (!hasPaidTicket)
        {
            // 🚨 ผู้โดยสารหนีตั๋ว — ผู้เล่นลืมเก็บเงิน
            Debug.Log($"{gameObject.name}: หวานเจี๊ยบ นั่งฟรีโว้ย! (ผู้โดยสารเนียนลงรถฟรี)");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMissedPassenger();
                GameManager.Instance.AdjustPopularity(-5f);
            }

            // ✅ แก้บั๊ก 3: ซ่อน moneyProp ก่อนลงรถ ไม่งั้น prop ลอยค้าง
            if (moneyProp != null) moneyProp.SetActive(false);

            // แสดงสีหน้าเยาะเย้ย
            SetMood(Mood.Happy);
        }
        else
        {
            // ✅ จ่ายเงินแล้ว — ลงรถตามปกติ
            Debug.Log($"{gameObject.name}: ลงรถเรียบร้อย ขอบคุณที่ใช้บริการ");
            SetMood(Mood.None);
        }
    }

    // ===============================
    // 🌟 Utility
    // ===============================

    /// <summary>
    /// เรียกจาก BusController หรือ StopManager เมื่อรถถึงป้ายที่ผู้โดยสารต้องลง
    /// </summary>
    public void TriggerExit()
    {
        // ✅ แก้บั๊ก 2: รับทุก state ที่ผู้โดยสารยังอยู่บนรถ
        // (WaitingForFare / HandExtended / Paying = ยังไม่จ่าย, Riding = จ่ายแล้ว)
        bool isOnBus = currentState == State.WaitingForFare ||
                       currentState == State.HandExtended    ||
                       currentState == State.Paying          ||
                       currentState == State.Riding;

        if (isOnBus)
            currentState = State.Exiting;
    }

    public void SetMood(Mood newMood)
    {
        if (moodIconRenderer == null) return;
        if (newMood == Mood.None) { moodIconRenderer.gameObject.SetActive(false); return; }

        moodIconRenderer.gameObject.SetActive(true);
        switch (newMood)
        {
            case Mood.Happy:   moodIconRenderer.sprite = iconHappy;   break;
            case Mood.Neutral: moodIconRenderer.sprite = iconNeutral; break;
            case Mood.Angry:   moodIconRenderer.sprite = iconAngry;   break;
        }
    }

    public Transform GetHandPosition()
    {
        if (!isSittingSeat) return handPosStand;
        return isRightSide ? handPosSitR : handPosSitL;
    }

    public string GetPromptText()
    {
        if (CanResolveRandomEvent())
        {
            if (activeRandomEvent == RandomEventType.DrunkDance)
                return "กด E เพื่อเตือนคนเมาให้หยุดป่วน";

            if (activeRandomEvent == RandomEventType.LoudPhone)
                return "กด E เพื่อเตือนให้คุยโทรศัพท์เบา ๆ";
        }

        if (currentState == State.WaitingForFare)  return "กด E เพื่อรับเงิน";
        if (currentState == State.HandExtended)    return "คลิกเพื่อรับเงิน";
        return "";
    }

    public void SetSeat(Transform seatPoint)
    {
        mySeatPoint = seatPoint;
        isRightSide = seatPoint.name.Contains("Sit_R") || seatPoint.name.Contains("_R");
        assignedSeat = BusSeat.ResolveSeatForPoint(seatPoint);
    }

    public int GetSeatTipBonus()
    {
        return assignedSeat != null ? assignedSeat.GetTipBonus() : 0;
    }

    float GetPatienceDecayMultiplier()
    {
        return assignedSeat != null ? assignedSeat.GetPatienceDecayMultiplier() : 1f;
    }

    public bool CanReceiveRandomEvent()
    {
        return currentState == State.Riding &&
               hasPaidTicket &&
               !isProcessingPayment &&
               activeRandomEvent == RandomEventType.None;
    }

    public RandomEventType GetActiveRandomEvent()
    {
        return activeRandomEvent;
    }

    public bool TryStartRandomEvent(RandomEventType eventType)
    {
        if (eventType == RandomEventType.None || !CanReceiveRandomEvent())
            return false;

        activeRandomEvent = eventType;
        CaptureSeatPose();

        switch (activeRandomEvent)
        {
            case RandomEventType.ToxicSmell:
                SetToxicState(true);
                SetMood(Mood.Angry);
                break;
            case RandomEventType.DrunkDance:
                SetMood(Mood.Happy);
                break;
            case RandomEventType.LoudPhone:
                SetMood(Mood.Neutral);
                break;
        }

        PlayRandomEventAnimation();
        PlayRandomEventAudio();

        return true;
    }

    public bool CanDebugReceiveRandomEvent()
    {
        return activeRandomEvent == RandomEventType.None &&
               currentState != State.Boarding &&
               currentState != State.FindingSeat &&
               currentState != State.Exiting;
    }

    public bool DebugForceRandomEvent(RandomEventType eventType)
    {
        if (eventType == RandomEventType.None || !CanDebugReceiveRandomEvent())
            return false;

        activeRandomEvent = eventType;
        CaptureSeatPose();

        switch (activeRandomEvent)
        {
            case RandomEventType.ToxicSmell:
                SetToxicState(true);
                SetMood(Mood.Angry);
                break;
            case RandomEventType.DrunkDance:
                SetMood(Mood.Happy);
                break;
            case RandomEventType.LoudPhone:
                SetMood(Mood.Neutral);
                break;
        }

        PlayRandomEventAnimation();
        PlayRandomEventAudio();
        return true;
    }

    public bool CanResolveRandomEvent()
    {
        return currentState == State.Riding &&
               (activeRandomEvent == RandomEventType.DrunkDance || activeRandomEvent == RandomEventType.LoudPhone);
    }

    public void ResolveRandomEvent()
    {
        if (!CanResolveRandomEvent())
            return;

        FinishRandomEvent(true);
    }

    void RecoverStalledPaymentState()
    {
        if (currentState != State.Paying || hasPaidTicket || isProcessingPayment)
            return;

        bool hasOwnActiveTransaction = fareSystem != null &&
                                       fareSystem.currentPassenger == this &&
                                       fareSystem.HasActiveTransaction();
        if (hasOwnActiveTransaction)
            return;

        currentState = State.WaitingForFare;

        if (animator != null)
        {
            if (isSittingSeat)
                animator.SetTrigger(isRightSide ? "trigSitDoneR" : "trigSitDoneL");
            else
                animator.SetTrigger("trigStandDone");
        }

        if (moneyProp != null)
            moneyProp.SetActive(false);
    }

    void CaptureSeatPose()
    {
        seatedBasePosition = transform.position;
        seatedBaseRotation = transform.rotation;
        hasSeatedBasePose = true;
    }

    void UpdateRandomEventMotion()
    {
        if (!hasSeatedBasePose || activeRandomEvent == RandomEventType.None || currentState != State.Riding)
            return;

        float phase = Time.time * eventAnimationSpeed;
        Vector3 positionOffset = Vector3.zero;
        Quaternion rotationOffset = Quaternion.identity;

        switch (activeRandomEvent)
        {
            case RandomEventType.DrunkDance:
                positionOffset = transform.up * (Mathf.Sin(phase * 1.6f) * drunkDanceBobAmplitude);
                rotationOffset = Quaternion.Euler(0f, Mathf.Sin(phase * 2.1f) * drunkDanceYawAmplitude, Mathf.Sin(phase) * 4f);
                break;

            case RandomEventType.LoudPhone:
                rotationOffset = Quaternion.Euler(0f, Mathf.Sin(phase * 1.4f) * 6f, Mathf.Sin(phase * 0.9f) * loudPhoneTiltAmplitude);
                break;
        }

        transform.position = seatedBasePosition + positionOffset;
        transform.rotation = seatedBaseRotation * rotationOffset;
    }

    void FinishRandomEvent(bool resolved)
    {
        if (activeRandomEvent == RandomEventType.None)
            return;

        StopRandomEventAnimation();
        StopRandomEventAudio();
        activeRandomEvent = RandomEventType.None;
        SetToxicState(false);
        SetMood(Mood.None);

        if (hasSeatedBasePose && currentState != State.Exiting)
        {
            transform.position = seatedBasePosition;
            transform.rotation = seatedBaseRotation;
        }

        onRandomEventFinished?.Invoke(this, resolved);
        onRandomEventFinished = null;
    }

    void EnsureRandomEventAudioSource()
    {
        if (randomEventAudioSource != null)
            return;

        randomEventAudioSource = GetComponent<AudioSource>();
        if (randomEventAudioSource == null)
            randomEventAudioSource = gameObject.AddComponent<AudioSource>();

        randomEventAudioSource.playOnAwake = false;
        randomEventAudioSource.loop = loopRandomEventAudio;
        randomEventAudioSource.spatialBlend = 1f;
        randomEventAudioSource.minDistance = 1.2f;
        randomEventAudioSource.maxDistance = 8f;
        randomEventAudioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    void PlayRandomEventAudio()
    {
        EnsureRandomEventAudioSource();
        if (randomEventAudioSource == null)
            return;

        AudioClip clip = GetRandomEventAudioClip();
        if (clip == null)
            return;

        randomEventAudioSource.clip = clip;
        randomEventAudioSource.volume = randomEventAudioVolume;
        randomEventAudioSource.loop = loopRandomEventAudio;
        randomEventAudioSource.Play();
    }

    void StopRandomEventAudio()
    {
        if (randomEventAudioSource == null)
            return;

        if (randomEventAudioSource.isPlaying)
            randomEventAudioSource.Stop();

        randomEventAudioSource.clip = null;
    }

    AudioClip GetRandomEventAudioClip()
    {
        return activeRandomEvent switch
        {
            RandomEventType.DrunkDance => drunkDanceAudioClip,
            RandomEventType.LoudPhone => loudPhoneAudioClip,
            _ => null
        };
    }

    void PlayRandomEventAnimation()
    {
        Animator targetAnimator = randomEventAnimator != null ? randomEventAnimator : animator;
        if (targetAnimator == null)
            return;

        switch (activeRandomEvent)
        {
            case RandomEventType.DrunkDance:
                bool handledDrunkDanceByParameters =
                    SetAnimatorBoolIfExists(targetAnimator, drunkDanceBoolName, true) |
                    SetAnimatorTriggerIfExists(targetAnimator, drunkDanceTriggerName);

                isUsingDirectDrunkDanceState = !handledDrunkDanceByParameters &&
                    TryCrossFadeToAnimatorState(targetAnimator, drunkDanceStateName);
                break;

            case RandomEventType.LoudPhone:
                SetAnimatorBoolIfExists(targetAnimator, loudPhoneBoolName, true);
                SetAnimatorTriggerIfExists(targetAnimator, loudPhoneTriggerName);
                break;
        }
    }

    void StopRandomEventAnimation()
    {
        Animator targetAnimator = randomEventAnimator != null ? randomEventAnimator : animator;
        if (targetAnimator == null)
            return;

        switch (activeRandomEvent)
        {
            case RandomEventType.DrunkDance:
                bool handledDrunkDanceByParameters =
                    SetAnimatorBoolIfExists(targetAnimator, drunkDanceBoolName, false) |
                    SetAnimatorTriggerIfExists(targetAnimator, drunkDanceStopTriggerName);

                if (isUsingDirectDrunkDanceState || !handledDrunkDanceByParameters)
                    TryCrossFadeToAnimatorState(targetAnimator, GetRandomEventRecoveryStateName());

                isUsingDirectDrunkDanceState = false;
                break;

            case RandomEventType.LoudPhone:
                SetAnimatorBoolIfExists(targetAnimator, loudPhoneBoolName, false);
                SetAnimatorTriggerIfExists(targetAnimator, loudPhoneStopTriggerName);
                break;
        }
    }

    string GetRandomEventRecoveryStateName()
    {
        return isSittingSeat ? sittingRecoveryStateName : standingRecoveryStateName;
    }

    bool TryCrossFadeToAnimatorState(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int layerIndex = GetAnimatorLayerIndex(targetAnimator, stateName);
        int stateHash = Animator.StringToHash(stateName);
        if (!targetAnimator.HasState(layerIndex, stateHash))
            return false;

        targetAnimator.CrossFadeInFixedTime(stateHash, randomEventStateCrossFadeDuration, layerIndex);
        return true;
    }

    int GetAnimatorLayerIndex(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return 0;

        int separatorIndex = stateName.IndexOf('.');
        if (separatorIndex <= 0)
            return 0;

        string layerName = stateName.Substring(0, separatorIndex);
        int layerIndex = targetAnimator.GetLayerIndex(layerName);
        return layerIndex >= 0 ? layerIndex : 0;
    }

    bool SetAnimatorBoolIfExists(Animator targetAnimator, string parameterName, bool value)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(parameterName) || !HasAnimatorParameter(targetAnimator, parameterName, AnimatorControllerParameterType.Bool))
            return false;

        targetAnimator.SetBool(parameterName, value);
        return true;
    }

    bool SetAnimatorTriggerIfExists(Animator targetAnimator, string parameterName)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(parameterName) || !HasAnimatorParameter(targetAnimator, parameterName, AnimatorControllerParameterType.Trigger))
            return false;

        targetAnimator.SetTrigger(parameterName);
        return true;
    }

    bool HasAnimatorParameter(Animator targetAnimator, string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == parameterType && parameters[i].name == parameterName)
                return true;
        }

        return false;
    }

    void OnDestroy()
    {
        FinishRandomEvent(false);
        onExitBus?.Invoke();
        onExitBus = null;
    }
}
