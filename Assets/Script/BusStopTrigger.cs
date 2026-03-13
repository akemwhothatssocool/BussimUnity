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
    private bool _hasTriggeredSpawn = false; // 🌟 เพิ่มตัวแปรเช็คการเสกคน

    void Update()
    {
        float dist = Mathf.Abs(transform.position.x - busStopX);

        // 1. ระบบเช็คจุดเกิด: เปลี่ยนมาเรียก TriggerSpawn (เสกเป็นกลุ่ม)
        if (dist >= spawnDistance && !_hasTriggeredSpawn && !_hasTriggeredStop)
        {
            _hasTriggeredSpawn = true;
            busStopManager.TriggerSpawn(); // 🌟 สั่งเสกกลุ่มตามความนิยม (1-5 คน)
            Debug.Log("สั่งเสกกลุ่มผู้โดยสารที่ป้าย!");
        }

        // 2. ระบบเช็คจุดจอด
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

        busStopManager.StartBoarding();

        // 🌟 รอจนคนขึ้นหมด และเช็คว่าไม่มีใครกำลังเดินลง (Exiting)
        // (AnyPassengerExiting คือฟังก์ชันที่เราจะเพิ่มไว้ด้านล่างครับ)
        yield return new WaitUntil(() => busStopManager.IsBoardingFinished() && !AnyPassengerExiting());

        yield return new WaitForSeconds(1.0f);

        // ✅ แก้จาก GameMask เป็น GameManager ครับ!
        if (GameManager.Instance != null)
            GameManager.Instance.AddStop();

        cityManager.ResumeScroll();

        yield return new WaitUntil(() => Mathf.Abs(transform.position.x - busStopX) > stopThreshold * 5f);
        _hasTriggeredStop = false;
        _hasTriggeredSpawn = false;
    }

    // 🌟 เพิ่มฟังก์ชันนี้ไว้ใน BusStopTrigger.cs ด้วยนะครับ (วางไว้นอก IEnumerator)
    bool AnyPassengerExiting()
    {
        // หา NPC ทั้งหมดในฉากมาเช็คสถานะ
        PassengerAI[] passengers = Object.FindObjectsByType<PassengerAI>(FindObjectsSortMode.None);
        foreach (var p in passengers)
        {
            if (p.currentState == PassengerAI.State.Exiting) return true;
        }
        return false;
    }
}