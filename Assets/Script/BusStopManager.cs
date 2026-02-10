using UnityEngine;
using System.Collections.Generic;

public class BusStopManager : MonoBehaviour
{
    public GameObject passengerPrefab;
    public Transform spawnPoint;
    public Transform exitPoint;
    public Transform[] seatPoints;

    private HashSet<Transform> occupiedSeats = new HashSet<Transform>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            int available = seatPoints.Length - occupiedSeats.Count;
            Debug.Log($"🪑 ที่นั่งว่าง: {available}/{seatPoints.Length}");
            SpawnPassenger();
        }
    }

    public void SpawnPassenger()
    {
        if (passengerPrefab == null)
        {
            Debug.LogError("❌ Passenger Prefab เป็น None!");
            return;
        }
        if (seatPoints == null || seatPoints.Length == 0)
        {
            Debug.LogError("❌ Seat Points ว่างเปล่า!");
            return;
        }

        Transform availableSeat = GetAvailableSeat();
        if (availableSeat == null)
        {
            Debug.LogWarning("⚠️ ที่นั่งเต็มแล้ว!");
            return;
        }

        GameObject p = Instantiate(passengerPrefab, spawnPoint.position, Quaternion.identity);

        PassengerAI ai = p.GetComponent<PassengerAI>();
        if (ai == null)
        {
            Debug.LogError("❌ Passenger Prefab ไม่มี PassengerAI Script!");
            Destroy(p);
            return;
        }

        ai.SetSeat(availableSeat);
        ai.exitPoint = exitPoint;
        ai.isSittingSeat = true;

        occupiedSeats.Add(availableSeat);

        // ✅ Subscribe event
        ai.onExitBus += () => FreeSeat(availableSeat);

        Debug.Log($"✅ Spawn Passenger ที่ {availableSeat.name}, ฝั่ง {(ai.isRightSide ? "ขวา" : "ซ้าย")}");
    }

    Transform GetAvailableSeat()
    {
        List<Transform> availableSeats = new List<Transform>();

        foreach (Transform seat in seatPoints)
        {
            if (!occupiedSeats.Contains(seat))
            {
                availableSeats.Add(seat);
            }
        }

        if (availableSeats.Count == 0)
            return null;

        int randIndex = UnityEngine.Random.Range(0, availableSeats.Count); // ✅ ระบุ UnityEngine.Random
        return availableSeats[randIndex];
    }

    public void FreeSeat(Transform seat)
    {
        if (occupiedSeats.Contains(seat))
        {
            occupiedSeats.Remove(seat);
            Debug.Log($"🪑 ที่นั่ง {seat.name} ว่างแล้ว");
        }
    }
}
