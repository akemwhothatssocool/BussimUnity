using UnityEngine;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
    // [ลบ Transform handPosition ออกจากตรงนี้ เพราะเราจะรับค่าผ่าน Parameter แทน]

    [Header("รูปแบบเงินที่ NPC มี (ลาก Prefab มาใส่ให้ครบ)")]
    public GameObject[] moneyPrefabs;

    private GameObject currentMoneyObject;

    // แก้ไข: เพิ่ม Parameter 'Transform npcHand' เพื่อรับตำแหน่งมือของตัวที่เรียกใช้
    public void SpawnMoney(int ticketPrice, Transform npcHand)
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
            NPCMoneyItem itemScript = prefab.GetComponent<NPCMoneyItem>();
            if (itemScript != null)
            {
                if (itemScript.moneyValue >= ticketPrice)
                {
                    validOptions.Add(prefab);
                }
            }
        }

        if (validOptions.Count == 0)
        {
            Debug.LogWarning("ไม่มีเงินที่พอจ่ายค่าตั๋ว!");
            validOptions.AddRange(moneyPrefabs);
        }

        // 3. สุ่มหยิบเงิน
        int randomIndex = Random.Range(0, validOptions.Count);
        GameObject selectedMoney = validOptions[randomIndex];

        // 4. เช็คว่าส่งมือมาจริงไหมเพื่อกัน Error
        if (npcHand != null)
        {
            // สร้างเงินที่ตำแหน่งมือของ NPC ตัวนั้นๆ
            currentMoneyObject = Instantiate(selectedMoney, npcHand.position, npcHand.rotation);

            // (Option) ถ้าอยากให้เงินขยับตามมือ NPC ตลอดเวลา ให้ set parent ด้วย
            currentMoneyObject.transform.SetParent(npcHand);
        }
        else
        {
            Debug.LogError("หา handPosition ของ NPC ไม่เจอ! ลืมส่งค่ามาหรือเปล่า?");
        }
    }
}