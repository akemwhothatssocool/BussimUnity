using UnityEngine;
using System.Collections.Generic;

public class BusStopManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject passengerPrefab;
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Points")]
    public Transform[] seatPoints;
    public Transform[] standPoints;

    private HashSet<Transform> occupiedSeats = new HashSet<Transform>();
    private HashSet<Transform> occupiedStandPoints = new HashSet<Transform>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            SpawnPassenger();
        }
    }

    public void SpawnPassenger()
    {
        if (passengerPrefab == null || spawnPoint == null) return;

        // 1. หาที่นั่ง/ยืน
        Transform targetPoint = GetAvailableSeat();
        bool isSitting = true;

        if (targetPoint == null)
        {
            targetPoint = GetAvailableStandPoint();
            isSitting = false;
        }

        if (targetPoint == null) return; // รถเต็มทั้งนั่งและยืน

        // 2. สร้างตัวละคร
        GameObject p = Instantiate(passengerPrefab, spawnPoint.position, Quaternion.identity);
        PassengerAI ai = p.GetComponent<PassengerAI>();

        if (ai != null)
        {
            // ไม่ต้องมี ai.interactTextUI แล้ว
            ai.SetSeat(targetPoint);
            ai.exitPoint = exitPoint;
            ai.isSittingSeat = isSitting;

            // Subscribe event คืนที่นั่ง/จุดยืน
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
        }
    }

    Transform GetAvailableSeat()
    {
        if (seatPoints == null || seatPoints.Length == 0) return null;

        List<Transform> available = new List<Transform>();
        foreach (Transform t in seatPoints)
        {
            if (t != null && !occupiedSeats.Contains(t))
                available.Add(t);
        }

        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    Transform GetAvailableStandPoint()
    {
        if (standPoints == null || standPoints.Length == 0) return null;

        List<Transform> available = new List<Transform>();
        foreach (Transform t in standPoints)
        {
            if (t != null && !occupiedStandPoints.Contains(t))
                available.Add(t);
        }

        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    public void FreeSeat(Transform seat)
    {
        if (seat != null && occupiedSeats.Contains(seat))
            occupiedSeats.Remove(seat);
    }

    public void FreeStandPoint(Transform spot)
    {
        if (spot != null && occupiedStandPoints.Contains(spot))
            occupiedStandPoints.Remove(spot);
    }
}
