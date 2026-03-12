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

    [Header("ระยะเวลาการเสก")]
    [Tooltip("ระยะห่างที่ไกลพอจะเริ่มเสกคน (แปลว่าเพิ่งวาร์ปมาใหม่ เช่น 50)")]
    public float spawnDistance = 50f;

    private bool _hasTriggered = false;
    private PassengerAI _waitingPassenger;

    void Update()
    {
        float dist = Mathf.Abs(transform.position.x - busStopX);

        // 1. ระบบเช็คจุดเกิด: ถ้าป้ายอยู่ไกลจากรถมากๆ (เกิน spawnDistance) และยังไม่มีคนมารอ ให้เสกคน!
        if (dist >= spawnDistance && _waitingPassenger == null && !_hasTriggered)
        {
            _waitingPassenger = busStopManager.SpawnPassenger();
        }

        // 2. ระบบเช็คจุดจอด: ถ้าป้ายเลื่อนมาถึงหน้ารถ
        if (!_hasTriggered && dist <= stopThreshold)
        {
            _hasTriggered = true;
            StartCoroutine(HandleBusStop());
        }
    }

    IEnumerator HandleBusStop()
    {
        cityManager.PauseScroll();

        yield return new WaitUntil(() => Mathf.Abs(cityManager._currentSpeed) < 0.05f);

        // รถจอดสนิท สั่งให้คนขึ้นรถ
        if (_waitingPassenger != null)
        {
            _waitingPassenger.BoardBus();
            _waitingPassenger = null;

            // ✅ นับป้าย
            GameManager.Instance.AddStop();
        }

        yield return new WaitForSeconds(dwellTime);

        cityManager.ResumeScroll();

        yield return new WaitUntil(() =>
            Mathf.Abs(transform.position.x - busStopX) > stopThreshold * 3f);

        _hasTriggered = false;
    }
}