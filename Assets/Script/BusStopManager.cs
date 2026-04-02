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

    [Header("Popularity Spawn Settings")]
    public int maxPeoplePerStop = 5;

    [Header("Spawn Delay")]
    public float spawnDelay = 1.2f;

    [Header("Board Delay")]
    public float boardDelay = 1.2f;

    [Header("Spawn Spread")]
    public float spawnRadius = 1.5f;
    public float minWaitSpacing = 0.8f;
    public int maxSpawnPositionAttempts = 12;

    // ==========================
    // Internal Systems
    // ==========================

    private HashSet<Transform> occupiedSeats = new HashSet<Transform>();

    private Queue<PassengerAI> passengerQueue = new Queue<PassengerAI>();

    bool isBoarding = false;

    // ==========================
    // 🚏 เรียกเมื่อรถจอดป้าย
    // ==========================
    public void TriggerSpawn()
    {
        StartCoroutine(SpawnPassengersRoutine());
    }

    public void ResetForNextDay()
    {
        StopAllCoroutines();
        passengerQueue.Clear();
        occupiedSeats.Clear();
        isBoarding = false;
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

        if (targetPoint == null)
        {
            // รถเต็ม
            return null;
        }

        Vector3 spawnPos = GetAvailableWaitPosition();

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
            ai.isSittingSeat = true;

            if (GameManager.Instance != null)
                GameManager.Instance.AddPassenger();

            occupiedSeats.Add(targetPoint);

            ai.onExitBus += () =>
            {
                FreeSeat(targetPoint);
            };

            ai.WaitAtStop(spawnPos, spawnPoint.rotation, spawnPoint);
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
            if (t == null || occupiedSeats.Contains(t))
                continue;

            BusSeat seatData = BusSeat.ResolveSeatForPoint(t);
            if (seatData != null && !seatData.IsUsableForPassengers())
                continue;

            if (!occupiedSeats.Contains(t))
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

    Vector3 GetAvailableWaitPosition()
    {
        float spacing = Mathf.Max(0.2f, minWaitSpacing);
        float spacingSqr = spacing * spacing;
        List<Vector3> occupiedWaitPositions = GetCurrentWaitingPositions();

        for (int attempt = 0; attempt < Mathf.Max(1, maxSpawnPositionAttempts); attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = spawnPoint.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (IsFarEnoughFromWaitingPassengers(candidate, occupiedWaitPositions, spacingSqr))
                return candidate;
        }

        int fallbackIndex = occupiedWaitPositions.Count;
        float angle = fallbackIndex * 137.5f * Mathf.Deg2Rad;
        float distance = Mathf.Max(spacing, Mathf.Min(spawnRadius, spacing * (fallbackIndex + 1)));
        Vector3 fallbackOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        return spawnPoint.position + fallbackOffset;
    }

    List<Vector3> GetCurrentWaitingPositions()
    {
        List<Vector3> result = new List<Vector3>();
        PassengerAI[] waitingPassengers = Object.FindObjectsByType<PassengerAI>(FindObjectsSortMode.None);

        foreach (PassengerAI passenger in waitingPassengers)
        {
            if (passenger == null || passenger.transform.parent != spawnPoint)
                continue;

            result.Add(passenger.transform.position);
        }

        return result;
    }

    bool IsFarEnoughFromWaitingPassengers(Vector3 candidate, List<Vector3> occupiedWaitPositions, float minDistanceSqr)
    {
        for (int i = 0; i < occupiedWaitPositions.Count; i++)
        {
            Vector3 delta = candidate - occupiedWaitPositions[i];
            delta.y = 0f;

            if (delta.sqrMagnitude < minDistanceSqr)
                return false;
        }

        occupiedWaitPositions.Add(candidate);
        return true;
    }
}
