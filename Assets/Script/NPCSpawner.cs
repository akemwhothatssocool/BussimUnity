using UnityEngine;
using System.Collections.Generic; // ต้องเพิ่มบรรทัดนี้เพื่อใช้ List

public class NPCSpawner : MonoBehaviour
{
    [Header("จุดที่ NPC จะยื่นมือมา")]
    public Transform handPosition;

    [Header("รูปแบบเงินที่ NPC มี (ลาก Prefab มาใส่ให้ครบ)")]
    public GameObject[] moneyPrefabs; // ใส่เหรียญ 10, แบงค์ 20, 50, 100, 500, 1000

    private GameObject currentMoneyObject;

    // รับค่า ticketPrice เข้ามาเพื่อคำนวณ
    public void SpawnMoney(int ticketPrice)
    {
        // 1. ลบเงินเก่าทิ้ง (ถ้ามี)
        if (currentMoneyObject != null)
        {
            Destroy(currentMoneyObject);
        }

        // 2. คัดเลือกเงินที่ "จ่ายพอ"
        List<GameObject> validOptions = new List<GameObject>();

        foreach (GameObject prefab in moneyPrefabs)
        {
            // ดึงค่าเงินจาก Prefab มาเช็ค
            NPCMoneyItem itemScript = prefab.GetComponent<NPCMoneyItem>();

            if (itemScript != null)
            {
                // กติกา: ต้องเป็นเงินที่มากกว่าหรือเท่ากับค่าตั๋ว
                // (เช่น ตั๋ว 15 -> เอาเฉพาะแบงค์ 20, 50, 100... ไม่เอาเหรียญ 10)
                if (itemScript.moneyValue >= ticketPrice)
                {
                    validOptions.Add(prefab);
                }
            }
        }

        // 3. ถ้าไม่มีเงินที่พอจ่ายเลย (กัน Error) ให้เอาใบที่ใหญ่ที่สุดที่มี
        if (validOptions.Count == 0)
        {
            Debug.LogWarning("ไม่มีเงินที่พอจ่ายค่าตั๋ว! ระบบจะสุ่มมั่วแทน");
            validOptions.AddRange(moneyPrefabs);
        }

        // 4. สุ่มหยิบจากตัวเลือกที่คัดมาแล้ว
        int randomIndex = Random.Range(0, validOptions.Count);
        GameObject selectedMoney = validOptions[randomIndex];

        // 5. สร้างเงินออกมา
        currentMoneyObject = Instantiate(selectedMoney, handPosition.position, handPosition.rotation);
    }
}