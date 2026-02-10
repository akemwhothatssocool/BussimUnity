using UnityEngine;
using System.Collections.Generic;

public class BusStopManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject passengerPrefab;
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Points")]
    public Transform[] seatPoints;  // จุดนั่ง
    public Transform[] standPoints; // จุดยืน (เพิ่มอันนี้)

    // ตัวเก็บว่าตรงไหนมีคนอยู่แล้ว
    private HashSet<Transform> occupiedSeats = new HashSet<Transform>();
    private HashSet<Transform> occupiedStandPoints = new HashSet<Transform>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            int seatsFree = seatPoints.Length - occupiedSeats.Count;
            int standsFree = standPoints.Length - occupiedStandPoints.Count;
            Debug.Log($"📊 สถานะ: นั่งว่าง {seatsFree} | ยืนว่าง {standsFree}");

            SpawnPassenger();
        }
    }

    public void SpawnPassenger()
    {
        if (passengerPrefab == null) { Debug.LogError("❌ Passenger Prefab เป็น None!"); return; }

        // 1. หาที่นั่งก่อน
        Transform targetPoint = GetAvailableSeat();
        bool isSitting = true;

        // 2. ถ้าที่นั่งเต็ม -> ไปหาที่ยืนแทน
        if (targetPoint == null)
        {
            targetPoint = GetAvailableStandPoint();
            isSitting = false; // บอกว่าเป็นท่ายืนนะ
        }

        // 3. ถ้าทั้งนั่งและยืนเต็มหมด -> จบข่าว
        if (targetPoint == null)
        {
            Debug.LogWarning("⚠️ รถเต็มแล้ว! (ไม่มีที่นั่งและที่ยืน)");
            return;
        }

        // --- เริ่มสร้างตัวละคร ---
        GameObject p = Instantiate(passengerPrefab, spawnPoint.position, Quaternion.identity);
        PassengerAI ai = p.GetComponent<PassengerAI>();

        if (ai == null)
        {
            Debug.LogError("❌ Passenger Prefab ไม่มี PassengerAI Script!");
            Destroy(p);
            return;
        }

        // ส่งข้อมูลให้ AI
        ai.SetSeat(targetPoint); // ส่งตำแหน่งเป้าหมาย (ไม่ว่าจะนั่งหรือยืน)
        ai.exitPoint = exitPoint;
        ai.isSittingSeat = isSitting; // *** สำคัญ: บอก AI ว่าต้องนั่งหรือยืน ***

        // จองที่
        if (isSitting)
        {
            occupiedSeats.Add(targetPoint);
            // ถ้าเป็นคนนั่ง -> ตอนลงรถให้คืนที่นั่ง
            ai.onExitBus += () => FreeSeat(targetPoint);
        }
        else
        {
            occupiedStandPoints.Add(targetPoint);
            // ถ้าเป็นคนยืน -> ตอนลงรถให้คืนที่ยืน
            ai.onExitBus += () => FreeStandPoint(targetPoint);
        }

        Debug.Log($"✅ ผู้โดยสารใหม่: {(isSitting ? "นั่ง" : "ยืน")} ที่ {targetPoint.name}");
    }

    // ฟังก์ชันหาที่นั่งว่าง (สุ่ม)
    Transform GetAvailableSeat()
    {
        List<Transform> available = new List<Transform>();
        foreach (Transform t in seatPoints)
        {
            if (!occupiedSeats.Contains(t)) available.Add(t);
        }

        if (available.Count == 0) return null;
        return available[UnityEngine.Random.Range(0, available.Count)];
    }

    // ฟังก์ชันหาที่ยืนว่าง (สุ่ม) - เพิ่มใหม่
    Transform GetAvailableStandPoint()
    {
        if (standPoints == null) return null;

        List<Transform> available = new List<Transform>();
        foreach (Transform t in standPoints)
        {
            if (!occupiedStandPoints.Contains(t)) available.Add(t);
        }

        if (available.Count == 0) return null;
        return available[UnityEngine.Random.Range(0, available.Count)];
    }

    // คืนที่นั่ง
    public void FreeSeat(Transform seat)
    {
        if (occupiedSeats.Contains(seat))
        {
            occupiedSeats.Remove(seat);
            Debug.Log($"🪑 ที่นั่ง {seat.name} ว่างแล้ว");
        }
    }

    // คืนที่ยืน - เพิ่มใหม่
    public void FreeStandPoint(Transform spot)
    {
        if (occupiedStandPoints.Contains(spot))
        {
            occupiedStandPoints.Remove(spot);
            Debug.Log($"👣 จุดยืน {spot.name} ว่างแล้ว");
        }
    }
}