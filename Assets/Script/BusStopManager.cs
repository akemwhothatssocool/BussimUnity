using UnityEngine;
using System.Collections.Generic;

public class BusStopManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject passengerPrefab;
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("ระบบรถ")]
    public CityManager cityManager;

    [Header("Points")]
    public Transform[] seatPoints;
    public Transform[] standPoints;

    private HashSet<Transform> occupiedSeats = new HashSet<Transform>();
    private HashSet<Transform> occupiedStandPoints = new HashSet<Transform>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            PassengerAI newPassenger = SpawnPassenger();
            if (newPassenger != null) newPassenger.BoardBus(); // ถ้ากด K ให้เดินขึ้นรถเลย
        }
    }

    // ✅ แก้ไข: เปลี่ยนจาก void เป็น PassengerAI เพื่อส่งตัวละครออกไปให้ป้ายสั่งงาน
    public PassengerAI SpawnPassenger()
    {
        if (passengerPrefab == null || spawnPoint == null) return null;

        Transform targetPoint = GetAvailableSeat();
        bool isSitting = true;

        if (targetPoint == null)
        {
            targetPoint = GetAvailableStandPoint();
            isSitting = false;
        }

        if (targetPoint == null) return null;

        GameObject p = Instantiate(passengerPrefab, spawnPoint.position, spawnPoint.rotation);
        PassengerAI ai = p.GetComponent<PassengerAI>();

        if (ai != null)
        {
            ai.cityManager = cityManager;
            ai.SetSeat(targetPoint);
            ai.exitPoint = exitPoint;
            ai.isSittingSeat = isSitting;

            if (isSitting)
            {
                occupiedSeats.Add(targetPoint);
                ai.onExitBus += () => FreeSeat(targetPoint);
            }
            else
            {
                occupiedStandPoints.Add(targetPoint);
                ai.onExitBus += () => FreeStandPoint(targetPoint);
            }

            // ✅ สั่งให้ยืนรอที่ป้ายทันทีที่เกิด
            ai.WaitAtStop(spawnPoint);
        }

        return ai; // ส่งตัว AI กลับไป
    }

    Transform GetAvailableSeat()
    {
        if (seatPoints == null || seatPoints.Length == 0) return null;
        List<Transform> available = new List<Transform>();
        foreach (Transform t in seatPoints)
            if (t != null && !occupiedSeats.Contains(t))
                available.Add(t);
        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    Transform GetAvailableStandPoint()
    {
        if (standPoints == null || standPoints.Length == 0) return null;
        List<Transform> available = new List<Transform>();
        foreach (Transform t in standPoints)
            if (t != null && !occupiedStandPoints.Contains(t))
                available.Add(t);
        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    public void FreeSeat(Transform seat)
    {
        if (seat != null && occupiedSeats.Contains(seat)) occupiedSeats.Remove(seat);
    }

    public void FreeStandPoint(Transform spot)
    {
        if (spot != null && occupiedStandPoints.Contains(spot)) occupiedStandPoints.Remove(spot);
    }
}