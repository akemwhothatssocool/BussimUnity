using UnityEngine;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
    [Header("ตั้งค่า NPC")]
    public GameObject npcPrefab; // อย่าลืมลาก Prefab ของ NPC มาใส่ใน Inspector

    [Header("รูปแบบเงินที่ NPC มี (ลาก Prefab มาใส่ให้ครบ)")]
    public GameObject[] moneyPrefabs;

    private GameObject currentMoneyObject;

    // --- ส่วนที่ 1: การเสกเงินใส่มือ NPC ---
    public void SpawnMoney(int ticketPrice, Transform npcHand)
    {
        // 1. เช็คก่อนว่าส่งมือมาจริงไหม
        if (npcHand == null)
        {
            Debug.LogError("หา handPosition ของ NPC ไม่เจอ! ลืมส่งค่ามาหรือเปล่า?");
            return;
        }

        // 2. ลบเงินเก่าทิ้ง (ถ้ามี)
        if (currentMoneyObject != null)
        {
            Destroy(currentMoneyObject);
        }
        
        // 3. คัดเลือกเงินที่ "จ่ายพอ"
        List<GameObject> validOptions = new List<GameObject>();

        foreach (GameObject prefab in moneyPrefabs)
        {
            NPCMoneyItem itemScript = prefab.GetComponent<NPCMoneyItem>();
            if (itemScript != null)
            {
                if (itemScript.moneyValue >= ticketPrice)
                {
                    validOptions.Add(prefab);
                }
            }
        }

        // ถ้าไม่มีเงินอันไหนพอเลย ให้หยิบอันที่มีทั้งหมดมาเป็นตัวเลือก (กัน Error)
        if (validOptions.Count == 0)
        {
            Debug.LogWarning("ไม่มีเงินที่พอจ่ายค่าตั๋ว! ใช้เงินที่มีทั้งหมดแทน");
            validOptions.AddRange(moneyPrefabs);
        }

        // 4. สุ่มหยิบเงินและสร้าง (Instantiate)
        int randomIndex = Random.Range(0, validOptions.Count);
        GameObject selectedMoney = validOptions[randomIndex];

        currentMoneyObject = Instantiate(selectedMoney, npcHand.position, npcHand.rotation);

        // ให้เงินขยับตามมือ NPC
        currentMoneyObject.transform.SetParent(npcHand);
    }

    // --- ส่วนที่ 2: การเสก NPC ที่ป้ายรถเมล์ ---
    public void SpawnPassengersAtStop(int amountToSpawn, Transform busStopLocation)
    {
        if (npcPrefab == null)
        {
            Debug.LogError("ลืมใส่ npcPrefab ใน NPCSpawner หรือเปล่า?");
            return;
        }

        for (int i = 0; i < amountToSpawn; i++)
        {
            // 1. สุ่มตำแหน่งรอบๆ จุดเกิด (รัศมี 2 เมตร)
            Vector2 randomCircle = Random.insideUnitCircle * 2f;
            Vector3 spawnPos = busStopLocation.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // 2. สร้าง NPC ตรงตำแหน่งที่สุ่มได้
            Instantiate(npcPrefab, spawnPos, busStopLocation.rotation);
        }
    }
}