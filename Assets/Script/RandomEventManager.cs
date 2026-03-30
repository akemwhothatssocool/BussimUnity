using UnityEngine;

public class RandomEventManager : MonoBehaviour
{
    public static RandomEventManager Instance;

    [Header("Timing")]
    public float minEventInterval = 30f;
    public float maxEventInterval = 60f;
    public float minEventDuration = 12f;
    public float maxEventDuration = 22f;

    [Header("Penalty")]
    public float penaltyTickInterval = 6f;
    public float toxicPenaltyPerTick = -1f;
    public float drunkPenaltyPerTick = -1.5f;
    public float loudPhonePenaltyPerTick = -1.25f;
    public float resolveReward = 2f;

    PassengerAI activePassenger;
    PassengerAI.RandomEventType activeEventType = PassengerAI.RandomEventType.None;
    float nextEventTime;
    float activeEventEndTime;
    float nextPenaltyTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ScheduleNextEvent();
    }

    void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (activePassenger != null)
        {
            UpdateActiveEvent();
            return;
        }

        if (Time.time >= nextEventTime)
            TryStartRandomEvent();
    }

    public static RandomEventManager GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        RandomEventManager existing = FindFirstObjectByType<RandomEventManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject managerObject = new GameObject("RandomEventManager");
        Instance = managerObject.AddComponent<RandomEventManager>();
        return Instance;
    }

    void TryStartRandomEvent()
    {
        PassengerAI[] passengers = FindObjectsByType<PassengerAI>(FindObjectsSortMode.None);
        if (passengers == null || passengers.Length == 0)
        {
            ScheduleNextEvent(10f, 18f);
            return;
        }

        System.Collections.Generic.List<PassengerAI> candidates = new System.Collections.Generic.List<PassengerAI>(passengers.Length);
        for (int i = 0; i < passengers.Length; i++)
        {
            PassengerAI passenger = passengers[i];
            if (passenger != null && passenger.CanReceiveRandomEvent())
                candidates.Add(passenger);
        }

        if (candidates.Count == 0)
        {
            ScheduleNextEvent(8f, 16f);
            return;
        }

        PassengerAI passengerTarget = candidates[Random.Range(0, candidates.Count)];
        PassengerAI.RandomEventType eventType = GetRandomEventType();
        if (!passengerTarget.TryStartRandomEvent(eventType))
        {
            ScheduleNextEvent(8f, 16f);
            return;
        }

        activePassenger = passengerTarget;
        activeEventType = eventType;
        activePassenger.onRandomEventFinished += HandleRandomEventFinished;
        activeEventEndTime = Time.time + Random.Range(minEventDuration, maxEventDuration);
        nextPenaltyTime = Time.time + penaltyTickInterval;

        Debug.Log($"Random event started: {activeEventType} on {activePassenger.name}");
    }

    void UpdateActiveEvent()
    {
        if (activePassenger == null)
        {
            ClearActiveEvent(false);
            return;
        }

        if (activePassenger.GetActiveRandomEvent() == PassengerAI.RandomEventType.None)
        {
            ClearActiveEvent(false);
            return;
        }

        if (Time.time >= nextPenaltyTime)
        {
            ApplyPenaltyTick();
            nextPenaltyTime = Time.time + penaltyTickInterval;
        }

        if (Time.time >= activeEventEndTime)
            ClearActiveEvent(false);
    }

    void ApplyPenaltyTick()
    {
        if (GameManager.Instance == null)
            return;

        float delta = activeEventType switch
        {
            PassengerAI.RandomEventType.ToxicSmell => toxicPenaltyPerTick,
            PassengerAI.RandomEventType.DrunkDance => drunkPenaltyPerTick,
            PassengerAI.RandomEventType.LoudPhone => loudPhonePenaltyPerTick,
            _ => 0f
        };

        if (Mathf.Abs(delta) > 0.001f)
            GameManager.Instance.AdjustPopularity(delta);
    }

    void HandleRandomEventFinished(PassengerAI passenger, bool resolved)
    {
        if (passenger != activePassenger)
            return;

        if (resolved && GameManager.Instance != null)
            GameManager.Instance.AdjustPopularity(resolveReward);

        ClearActiveEvent(resolved);
    }

    void ClearActiveEvent(bool resolved)
    {
        if (activePassenger != null)
            activePassenger.onRandomEventFinished -= HandleRandomEventFinished;

        activePassenger = null;
        activeEventType = PassengerAI.RandomEventType.None;
        activeEventEndTime = 0f;
        nextPenaltyTime = 0f;
        ScheduleNextEvent();

        if (resolved)
            Debug.Log("Random event resolved by player");
    }

    void ScheduleNextEvent(float? overrideMin = null, float? overrideMax = null)
    {
        float min = overrideMin ?? minEventInterval;
        float max = overrideMax ?? maxEventInterval;
        nextEventTime = Time.time + Random.Range(min, max);
    }

    PassengerAI.RandomEventType GetRandomEventType()
    {
        int roll = Random.Range(0, 100);
        if (roll < 34)
            return PassengerAI.RandomEventType.ToxicSmell;

        if (roll < 67)
            return PassengerAI.RandomEventType.DrunkDance;

        return PassengerAI.RandomEventType.LoudPhone;
    }
}
