using UnityEngine;
using System.Collections;

public class BusStopTrigger : MonoBehaviour
{
    [Header("เชื่อมกับ Manager")]
    public CityManager cityManager;
    public BusStopManager busStopManager;

    [Header("ตั้งค่า")]
    public float busStopX = 0f;
    public float stopThreshold = 2f;
    public float dwellTime = 5f;
    public float spawnDistance = 50f;

    private bool _hasTriggeredStop = false;
    private bool _hasTriggeredSpawn = false;

    void Update()
    {
        float dist = Mathf.Abs(transform.position.x - busStopX);

        if (dist >= spawnDistance && !_hasTriggeredSpawn && !_hasTriggeredStop)
        {
            _hasTriggeredSpawn = true;
            busStopManager.TriggerSpawn();
            Debug.Log("สั่งเสกกลุ่มผู้โดยสารที่ป้าย!");
        }

        if (!_hasTriggeredStop && dist <= stopThreshold)
        {
            _hasTriggeredStop = true;
            StartCoroutine(HandleBusStop());
        }
    }

    IEnumerator HandleBusStop()
    {
        cityManager.PauseScroll();
        yield return new WaitUntil(() => Mathf.Abs(cityManager._currentSpeed) < 0.05f);

        // ==========================================
        // 🌟 1. สั่งผู้โดยสารที่ถึงป้ายเป้าหมายลงรถ
        // ==========================================
        int currentStopNumber = GameManager.Instance.stopsReached + 1;

        PassengerAI[] passengersOnBus = Object.FindObjectsByType<PassengerAI>(FindObjectsSortMode.None);
        foreach (var p in passengersOnBus)
        {
            if (currentStopNumber < p.targetStop) continue;

            // ✅ แก้บั๊ก 1: เช็คทุก state ที่ผู้โดยสารอาจอยู่บนรถ
            // ไม่ว่าจะจ่ายเงินแล้วหรือยัง ถึงป้ายก็ต้องลง
            bool isOnBus = p.currentState == PassengerAI.State.WaitingForFare ||
                           p.currentState == PassengerAI.State.HandExtended    ||
                           p.currentState == PassengerAI.State.Paying          ||
                           p.currentState == PassengerAI.State.Riding;

            if (isOnBus)
            {
                // ✅ แก้บั๊ก 2: เรียก TriggerExit() แทนการตั้ง state ตรงๆ
                p.TriggerExit();
                Debug.Log($"{p.gameObject.name} ลงรถที่ป้าย {currentStopNumber} (จ่ายตั๋ว: {p.hasPaidTicket})");
            }
        }
        // ==========================================

        // 2. สั่งคนขึ้นรถ
        busStopManager.StartBoarding();

        // 3. รอจนกว่าทั้ง "คนขึ้น" และ "คนลง" ทำงานเสร็จหมด
        yield return new WaitUntil(() => busStopManager.IsBoardingFinished() && !AnyPassengerExiting());

        yield return new WaitForSeconds(3f);

        // 4. อัปเดตจำนวนป้าย
        if (GameManager.Instance != null)
            GameManager.Instance.AddStop();

        cityManager.ResumeScroll();

        yield return new WaitUntil(() => Mathf.Abs(transform.position.x - busStopX) > stopThreshold * 5f);
        _hasTriggeredStop = false;
        _hasTriggeredSpawn = false;
    }

    bool AnyPassengerExiting()
    {
        PassengerAI[] passengers = Object.FindObjectsByType<PassengerAI>(FindObjectsSortMode.None);
        foreach (var p in passengers)
        {
            if (p.currentState == PassengerAI.State.Exiting) return true;
        }
        return false;
    }
}
