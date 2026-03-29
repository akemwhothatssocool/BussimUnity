using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BusStopManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject passengerPrefab;
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("ระบบรถ")]
    public CityManager cityManager;

    [Header("Seat Points")]
    public Transform[] seatPoints;

    [Header("Stand Points")]
    public Transform[] standPoints;

    [Header("Popularity Spawn Settings")]
    public int maxPeoplePerStop = 5;

    [Header("Spawn Delay")]
    public float spawnDelay = 1.2f;

    [Header("Board Delay")]
    public float boardDelay = 1.2f;

    [Header("Spawn Spread")]
    public float spawnRadius = 1.5f;

    // ==========================
    // Internal Systems
    // ==========================

    private HashSet<Transform> occupiedSeats = new HashSet<Transform>();
    private HashSet<Transform> occupiedStandPoints = new HashSet<Transform>();

    private Queue<PassengerAI> passengerQueue = new Queue<PassengerAI>();

    bool isBoarding = false;

    // ==========================
    // 🚏 เรียกเมื่อรถจอดป้าย
    // ==========================
    public void TriggerSpawn()
    {
        StartCoroutine(SpawnPassengersRoutine());
    }

    IEnumerator SpawnPassengersRoutine()
    {
        if (GameManager.Instance == null) yield break;

        float popRate = GameManager.Instance.popularity / 100f;

        int peopleToSpawn = Mathf.RoundToInt(maxPeoplePerStop * popRate);

        peopleToSpawn = Mathf.Max(1, peopleToSpawn);

        for (int i = 0; i < peopleToSpawn; i++)
        {
            PassengerAI ai = SpawnPassenger();

            if (ai != null)
            {
                passengerQueue.Enqueue(ai);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // ==========================
    // Spawn Passenger
    // ==========================
    public PassengerAI SpawnPassenger()
    {
        if (passengerPrefab == null || spawnPoint == null)
            return null;

        Transform targetPoint = GetAvailableSeat();
        bool isSitting = true;

        if (targetPoint == null)
        {
            targetPoint = GetAvailableStandPoint();
            isSitting = false;
        }

        if (targetPoint == null)
        {
            // รถเต็ม
            return null;
        }

        // ⭐ spawn กระจายตำแหน่ง
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPos = spawnPoint.position + new Vector3(
            randomCircle.x,
            0,
            randomCircle.y
        );

        GameObject p = Instantiate(
            passengerPrefab,
            spawnPos,
            spawnPoint.rotation
        );

        PassengerAI ai = p.GetComponent<PassengerAI>();

        if (ai != null)
        {
            ai.cityManager = cityManager;
            ai.SetSeat(targetPoint);
            ai.exitPoint = exitPoint;
            ai.isSittingSeat = isSitting;

            if (GameManager.Instance != null)
                GameManager.Instance.AddPassenger();

            if (isSitting)
            {
                occupiedSeats.Add(targetPoint);

                ai.onExitBus += () =>
                {
                    FreeSeat(targetPoint);
                };
            }
            else
            {
                occupiedStandPoints.Add(targetPoint);

                ai.onExitBus += () =>
                {
                    FreeStandPoint(targetPoint);
                };
            }

            ai.WaitAtStop(spawnPoint);
        }

        return ai;
    }

    // ==========================
    // 🚶 ให้ขึ้นรถทีละคน
    // ==========================
    public void StartBoarding()
    {
        if (!isBoarding)
        {
            StartCoroutine(BoardPassengersRoutine());
        }
    }

    IEnumerator BoardPassengersRoutine()
    {
        isBoarding = true;

        while (passengerQueue.Count > 0)
        {
            PassengerAI nextPassenger = passengerQueue.Dequeue();

            if (nextPassenger != null)
            {
                nextPassenger.BoardBus();
            }

            yield return new WaitForSeconds(boardDelay);
        }

        isBoarding = false;
    }

    // ==========================
    // 🌟 เช็คว่าขึ้นรถหมดหรือยัง
    // ==========================
    public bool IsBoardingFinished()
    {
        return passengerQueue.Count == 0 && !isBoarding;
    }

    // ==========================
    // Seat System
    // ==========================
    Transform GetAvailableSeat()
    {
        List<Transform> available = new List<Transform>();

        foreach (Transform t in seatPoints)
        {
            if (t != null && !occupiedSeats.Contains(t))
            {
                available.Add(t);
            }
        }

        if (available.Count == 0)
            return null;

        return available[Random.Range(0, available.Count)];
    }

    // ==========================
    // Stand System
    // ==========================
    Transform GetAvailableStandPoint()
    {
        List<Transform> available = new List<Transform>();

        foreach (Transform t in standPoints)
        {
            if (t != null && !occupiedStandPoints.Contains(t))
            {
                available.Add(t);
            }
        }

        if (available.Count == 0)
            return null;

        return available[Random.Range(0, available.Count)];
    }

    // ==========================
    // Free Seat
    // ==========================
    public void FreeSeat(Transform seat)
    {
        if (seat != null && occupiedSeats.Contains(seat))
        {
            occupiedSeats.Remove(seat);
        }
    }

    // ==========================
    // Free Stand Point
    // ==========================
    public void FreeStandPoint(Transform spot)
    {
        if (spot != null && occupiedStandPoints.Contains(spot))
        {
            occupiedStandPoints.Remove(spot);
        }
    }
}
