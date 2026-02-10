using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FareSystem : MonoBehaviour
{
    [Header("ตั้งค่าหน้าจอ")]
    public GameObject uiPanel;
    public Text textPrice;
    public Text textReceived;
    public Text textChange;

    [Header("ข้อมูลระบบ")]
    public TicketData currentTicket;
    public int moneyReceived;
    public int currentChange;
    private bool isTransactionActive = false;

    [Header("NPC ปัจจุบัน")]
    public PassengerAI currentPassenger;
    private int currentPoseState = 0;

    [Header("ตัวจัดการ NPC")]
    public NPCSpawner npcSpawner;
    public Animator npcAnimator;

    [Header("ตำแหน่งมือ (นั่งซ้าย/ขวา)")]
    public Transform handPosSitL;
    public Transform handPosSitR;

    [Header("เสียงเอฟเฟกต์")]
    public AudioSource audioSource;
    public AudioClip sfxCoin;
    public AudioClip sfxBank;

    [Header("คลังคำศัพท์")]
    public string[] successQuotes = new string[] { "ขอบคุณครับ", "เชิญข้างในเลยครับ" };
    public string[] failUnderQuotes = new string[] { "เฮ้ย! ทอนไม่ครบป่าวพี่!!", "จะโกงเหรอ!" };
    public string[] failOverQuotes = new string[] { "โอ้โห หวานเจี๊ยบ!", "ขอบคุณเสี่ย!" };

    void Start()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void StartTransaction(PassengerAI passenger)
    {
        // 1. เก็บตัว NPC ไว้
        currentPassenger = passenger;
        npcAnimator = passenger.animator;

        // 2. ตั้งค่าตั๋ว/ราคา
        if (currentTicket == null)
            currentTicket = ScriptableObject.CreateInstance<TicketData>();
        currentTicket.price = 20;

        // 3. รีเซ็ตค่าเงิน
        moneyReceived = 0;
        currentChange = 0;
        isTransactionActive = true;

        // 4. เช็คท่าทาง (ห้ามมีบรรทัดสั่ง transform.position)
        if (!passenger.isSittingSeat)
        {
            currentPoseState = 0; // ยืน
            StartCoroutine(StandThenPayRoutine());
        }
        else if (!passenger.isRightSide) // นั่งซ้าย
        {
            currentPoseState = 1;
            StartCoroutine(SitThenPayRoutine("trigSitGiveL", handPosSitL));
        }
        else // นั่งขวา
        {
            currentPoseState = 2;
            StartCoroutine(SitThenPayRoutine("trigSitGiveR", handPosSitR));
        }

        // 5. ตั้งค่า UI
        if (uiPanel != null) uiPanel.SetActive(false);
        textPrice.color = Color.black;
        textPrice.text = "ค่ารถ: " + currentTicket.price + " บาท";

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    IEnumerator StandThenPayRoutine()
    {
        yield return new WaitForSeconds(0.1f);

        if (npcAnimator != null)
        {
            npcAnimator.SetTrigger("trigStandGive");
            Debug.Log("💰 Triggered: trigStandGive");
        }

        yield return new WaitForSeconds(0.5f);

        if (npcSpawner != null && currentPassenger != null)
        {
            Transform handPos = currentPassenger.GetHandPosition();
            if (handPos != null)
            {
                npcSpawner.handPosition.position = handPos.position;
                npcSpawner.handPosition.rotation = handPos.rotation;
                npcSpawner.SpawnMoney(currentTicket.price);
            }
        }
    }

    IEnumerator SitThenPayRoutine(string giveTriggerName, Transform handPosOverride)
    {
        yield return new WaitForSeconds(0.1f);

        if (npcAnimator != null)
        {
            npcAnimator.SetTrigger(giveTriggerName);
            Debug.Log($"💰 Triggered: {giveTriggerName}");
        }

        yield return new WaitForSeconds(0.5f);

        if (npcSpawner != null && currentPassenger != null)
        {
            // ใช้ตำแหน่งที่กำหนดสำหรับฝั่งซ้าย/ขวา (ไม่ยุ่งกับตำแหน่งตัว NPC)
            Transform handPos = handPosOverride != null ? handPosOverride : currentPassenger.GetHandPosition();
            if (handPos != null)
            {
                npcSpawner.handPosition.position = handPos.position;
                npcSpawner.handPosition.rotation = handPos.rotation;
                npcSpawner.SpawnMoney(currentTicket.price);
            }
        }
    }

    public void ReceiveNPCMoney(int amount)
    {
        moneyReceived += amount;
        PlayMoneySound(amount);

        if (npcAnimator != null)
        {
            if (currentPoseState == 0) npcAnimator.SetTrigger("trigStandDone");
            else if (currentPoseState == 1) npcAnimator.SetTrigger("trigSitDoneL");
            else npcAnimator.SetTrigger("trigSitDoneR");
        }

        if (uiPanel != null && !uiPanel.activeSelf) uiPanel.SetActive(true);
        UpdateUI();
    }

    public void AddChange(int amount)
    {
        if (!isTransactionActive) return;
        currentChange += amount;
        PlayMoneySound(amount);
        UpdateUI();
    }

    public void ClearChange()
    {
        if (!isTransactionActive) return;
        currentChange = 0;
        textPrice.color = Color.black;
        textPrice.text = "ค่ารถ: " + currentTicket.price + " บาท";
        UpdateUI();
    }

    public void SubmitTransaction()
    {
        if (!isTransactionActive) return;

        int correctChangeNeeded = moneyReceived - currentTicket.price;
        int diff = currentChange - correctChangeNeeded;

        if (moneyReceived >= currentTicket.price && diff >= 0)
        {
            isTransactionActive = false;
        }

        StartCoroutine(ShowResultAndClose(diff));
    }

    IEnumerator ShowResultAndClose(int diff)
    {
        if (moneyReceived < currentTicket.price)
        {
            textPrice.text = "ขาดเงิน " + (currentTicket.price - moneyReceived) + " บาท!";
            textPrice.color = Color.red;
            yield return new WaitForSeconds(2.0f);
            isTransactionActive = true;
            textPrice.text = "ยังขาด " + (currentTicket.price - moneyReceived) + " บาท";
            textPrice.color = Color.black;
        }
        else if (diff == 0)
        {
            textPrice.text = successQuotes[Random.Range(0, successQuotes.Length)];
            textPrice.color = Color.green;
            yield return new WaitForSeconds(2.0f);
            CloseTransaction();
        }
        else if (diff < 0)
        {
            textPrice.text = failUnderQuotes[Random.Range(0, failUnderQuotes.Length)] + "\n(ขาด " + Mathf.Abs(diff) + ")";
            textPrice.color = Color.red;
            yield return new WaitForSeconds(2.0f);
            isTransactionActive = true;
            textPrice.text = "ยังขาด " + Mathf.Abs(diff) + " บาท";
            textPrice.color = Color.black;
        }
        else
        {
            textPrice.text = failOverQuotes[Random.Range(0, failOverQuotes.Length)] + "\n(เกิน " + diff + ")";
            textPrice.color = Color.yellow;
            yield return new WaitForSeconds(3.0f);
            CloseTransaction();
        }
    }

    void CloseTransaction()
    {
        textPrice.text = "รอผู้โดยสาร...";
        textPrice.color = Color.black;
        moneyReceived = 0;
        currentChange = 0;
        isTransactionActive = false;

        if (uiPanel != null) uiPanel.SetActive(false);

        // --- ✅ เติมส่วนนี้: สั่งให้ Animation กลับไปนั่งเฉยๆ ---
        if (npcAnimator != null)
        {
            if (currentPoseState == 0) npcAnimator.SetTrigger("trigStandDone");
            else if (currentPoseState == 1) npcAnimator.SetTrigger("trigSitDoneL"); // ซ้ายกลับ
            else npcAnimator.SetTrigger("trigSitDoneR"); // ขวากลับ
        }
        // ----------------------------------------------------

        if (currentPassenger != null)
        {
            currentPassenger.PaymentCompleted();
            currentPassenger = null;
        }
    }

    void UpdateUI()
    {
        if (uiPanel != null && uiPanel.activeSelf)
        {
            if (textPrice.color == Color.black && currentTicket != null)
            {
                if (!textPrice.text.Contains("ขาด") && !textPrice.text.Contains("เกิน"))
                {
                    textPrice.text = "ค่ารถ: " + currentTicket.price + " บาท";
                }
            }

            if (textReceived != null)
                textReceived.text = "รับมา: " + moneyReceived;

            if (textChange != null)
                textChange.text = "เงินทอน: " + currentChange;
        }
    }

    void PlayMoneySound(int amount)
    {
        if (audioSource == null) return;

        if (amount <= 10)
            audioSource.PlayOneShot(sfxCoin);
        else
            audioSource.PlayOneShot(sfxBank);
    }
}
