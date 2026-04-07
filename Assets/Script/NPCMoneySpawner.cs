using UnityEngine;
using System.Collections.Generic;

public class NPCMoneySpawner : MonoBehaviour
{
    [Header("รูปแบบเงินที่ NPC มี (ลาก Prefab มาใส่ให้ครบ)")]
    public GameObject[] moneyPrefabs;

    private readonly List<GameObject> currentMoneyObjects = new List<GameObject>();

    // --- ส่วนที่ 1: การเสกเงินใส่มือ NPC ---
    public void SpawnMoney(int paymentAmount, Transform npcHand)
    {
        // 1. เช็คก่อนว่าส่งมือมาจริงไหม
        if (npcHand == null)
        {
            Debug.LogError("หา handPosition ของ NPC ไม่เจอ! ลืมส่งค่ามาหรือเปล่า?");
            return;
        }

        // 2. ลบเงินเก่าทิ้ง (ถ้ามี)
        ClearSpawnedMoney();

        Dictionary<int, List<GameObject>> prefabsByValue = BuildMoneyPrefabMap();
        if (prefabsByValue.Count == 0)
        {
            Debug.LogWarning("ไม่มี money prefab ที่ใช้งานได้ใน NPCMoneySpawner");
            return;
        }

        List<int> paymentPieces = BuildPaymentPieces(paymentAmount, prefabsByValue);
        if (paymentPieces.Count == 0)
        {
            Debug.LogWarning("ไม่สามารถจัดชุดเงินให้ NPC ได้");
            return;
        }

        for (int i = 0; i < paymentPieces.Count; i++)
        {
            int pieceValue = paymentPieces[i];
            List<GameObject> candidates = prefabsByValue[pieceValue];
            GameObject selectedMoney = candidates[Random.Range(0, candidates.Count)];
            Vector3 pieceOffset = (npcHand.right * (0.012f * i)) + (npcHand.up * (0.002f * i));
            GameObject spawnedMoney = Instantiate(selectedMoney, npcHand.position + pieceOffset, npcHand.rotation);
            spawnedMoney.transform.SetParent(npcHand);
            currentMoneyObjects.Add(spawnedMoney);
        }
    }

    Dictionary<int, List<GameObject>> BuildMoneyPrefabMap()
    {
        Dictionary<int, List<GameObject>> result = new Dictionary<int, List<GameObject>>();

        foreach (GameObject prefab in moneyPrefabs)
        {
            if (prefab == null) continue;

            NPCMoneyItem itemScript = prefab.GetComponent<NPCMoneyItem>();
            if (itemScript == null) continue;

            if (!result.TryGetValue(itemScript.moneyValue, out List<GameObject> options))
            {
                options = new List<GameObject>();
                result[itemScript.moneyValue] = options;
            }

            options.Add(prefab);
        }

        return result;
    }

    List<int> BuildPaymentPieces(int paymentAmount, Dictionary<int, List<GameObject>> prefabsByValue)
    {
        List<int> pieces = new List<int>();
        List<int> sortedValues = new List<int>(prefabsByValue.Keys);
        sortedValues.Sort((a, b) => b.CompareTo(a));

        int remaining = paymentAmount;
        foreach (int value in sortedValues)
        {
            while (remaining >= value)
            {
                pieces.Add(value);
                remaining -= value;
            }
        }

        if (remaining == 0 && pieces.Count > 0)
        {
            return pieces;
        }

        pieces.Clear();

        foreach (int value in sortedValues)
        {
            if (value >= paymentAmount)
            {
                pieces.Add(value);
                return pieces;
            }
        }

        if (sortedValues.Count > 0)
        {
            pieces.Add(sortedValues[0]);
        }

        return pieces;
    }

    void ClearSpawnedMoney()
    {
        foreach (GameObject moneyObject in currentMoneyObjects)
        {
            if (moneyObject != null)
            {
                Destroy(moneyObject);
            }
        }

        currentMoneyObjects.Clear();
    }
}
