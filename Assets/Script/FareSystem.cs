using System.Collections;
using UnityEngine;
using TMPro;

public class FareSystem : MonoBehaviour
{
    [Header("โมเดลมือขวา (ถือเงิน)")]
    public GameObject rightHandMoneyModel;

    [Header("⚙️ ตั้งค่าแอนิเมชันมือขวา (สไลด์ขึ้นลง)")]
    public float handAnimDuration = 0.3f;
    public Vector3 hiddenOffset = new Vector3(0f, -0.8f, 0f);

    private Vector3 visibleHandPos;
    private Vector3 hiddenHandPos;
    private Coroutine handCoroutine;

    [Header("แอนิเมชันกระบอกตั๋ว (มือซ้าย)")]
    public Animator cylinderAnimator;
    public string lidBoolName = "IsOpen";

    [Header("หน้าจอ UI ที่ต้องการให้ซ่อน/แสดงตอนคิดเงิน")]
    public GameObject[] paymentUIElements;

    [Header("ตัวเลขในแคปซูล (โชว์แค่ตัวเลข)")]
    public TextMeshProUGUI textPrice;
    public TextMeshProUGUI textReceived;
    public TextMeshProUGUI textChange;

    [Header("ข้อความแจ้งเตือน / คำพูด NPC")]
    public TextMeshProUGUI textStatus;

    [Header("ราคาค่าโดยสาร")]
    public int[] possiblePrices = { 8, 10, 12, 15, 20 };

    [Header("ข้อมูลระบบ")]
    public TicketData currentTicket;
    public int moneyReceived;
    public int currentChange;
    private bool isTransactionActive = false;
    private bool waitingForCollection = false;
    private int npcPlannedPayment = 0;

    [Header("NPC ปัจจุบัน")]
    public PassengerAI currentPassenger;
    private int currentPoseState = 0;

    [Header("ตัวจัดการ NPC")]
    public NPCSpawner npcSpawner;
    public Animator npcAnimator;

    [Header("ตำแหน่งมือ (นั่งซ้าย/ขวา)")]
    public Transform handPosSitL;
    public Transform handPosSitR;

    [Header("เสียงเอฟเฟกต์ (เงิน)")]
    public AudioSource audioSource;
    public AudioClip[] sfxCoins;
    public AudioClip[] sfxBanks;

    // 🌟 1. เพิ่มช่องใส่เสียงเวลาทอนเงิน 🌟
    [Header("เสียงสถานะการทอนเงิน")]
    public AudioClip[] sfxSuccess;    // เสียงทอนถูกเป๊ะ
    public AudioClip[] sfxFailUnder;  // เสียงทอนขาด (น้อยไป)
    public AudioClip[] sfxFailOver;   // เสียงทอนเกิน

    [Header("คลังคำศัพท์")]
    public string[] successQuotes = new string[] { "ขอบคุณครับ", "เชิญข้างในเลยครับ" };
    public string[] failUnderQuotes = new string[] { "เฮ้ย! ทอนไม่ครบป่าวพี่!!", "จะโกงเหรอ!" };
    public string[] failOverQuotes = new string[] { "โอ้โห หวานเจี๊ยบ!", "ขอบคุณเสี่ย!" };

    [Header("Player Controller")]
    public BusPlayerController playerController;

    void Start()
    {
        TogglePaymentUI(false);

        if (rightHandMoneyModel != null)
        {
            visibleHandPos = rightHandMoneyModel.transform.localPosition;
            hiddenHandPos = visibleHandPos + hiddenOffset;
            rightHandMoneyModel.transform.localPosition = hiddenHandPos;
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (playerController == null) playerController = FindFirstObjectByType<BusPlayerController>();

        ResetTextDisplay();
    }

    void Update()
    {
        if (waitingForCollection)
        {
            if (Input.GetMouseButtonDown(0))
            {
                waitingForCollection = false;
                OpenPaymentUI();
            }
            return;
        }

        if (!isTransactionActive) return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitTransaction();
        }

        if (Input.GetKeyDown(KeyCode.C) ||
            Input.GetKeyDown(KeyCode.Backspace))
        {
            ClearChange();
        }
    }

    int GenerateRealisticPayment(int price)
    {
        int[] bills = { 1000, 500, 100, 50, 20 };
        int strategy = Random.Range(0, 3);

        if (strategy == 0)
        {
            foreach (int bill in bills)
                if (bill >= price) return bill;
        }
        else if (strategy == 1)
        {
            return price;
        }
        else
        {
            int[] biggerBills = { 20, 50, 100, 500, 1000 };
            foreach (int bill in biggerBills)
                if (bill > price) return bill;
        }

        return price;
    }

    void TogglePaymentUI(bool isVisible)
    {
        if (paymentUIElements == null || paymentUIElements.Length == 0) return;
        foreach (GameObject ui in paymentUIElements)
            if (ui != null) ui.SetActive(isVisible);
    }

    public void AnimateHand(bool show)
    {
        if (rightHandMoneyModel == null) return;
        if (handCoroutine != null) StopCoroutine(handCoroutine);
        handCoroutine = StartCoroutine(HandSlideRoutine(show));
    }

    IEnumerator HandSlideRoutine(bool show)
    {
        Vector3 startPos = rightHandMoneyModel.transform.localPosition;
        Vector3 targetPos = show ? visibleHandPos : hiddenHandPos;
        float elapsedTime = 0f;

        while (elapsedTime < handAnimDuration)
        {
            rightHandMoneyModel.transform.localPosition = Vector3.Lerp(startPos, targetPos, elapsedTime / handAnimDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        rightHandMoneyModel.transform.localPosition = targetPos;
    }

    public void StartTransaction(PassengerAI passenger)
    {
        currentPassenger = passenger;
        npcAnimator = passenger.animator;

        if (currentTicket == null)
            currentTicket = ScriptableObject.CreateInstance<TicketData>();

        currentTicket.price = possiblePrices[Random.Range(0, possiblePrices.Length)];
        npcPlannedPayment = GenerateRealisticPayment(currentTicket.price);

        Debug.Log($"ราคา: {currentTicket.price} ฿ | NPC จ่าย: {npcPlannedPayment} ฿ | ต้องทอน: {npcPlannedPayment - currentTicket.price} ฿");

        moneyReceived = 0;
        currentChange = 0;

        AnimateHand(true);

        if (cylinderAnimator != null)
            cylinderAnimator.SetBool(lidBoolName, true);

        if (!passenger.isSittingSeat)
        {
            currentPoseState = 0;
            StartCoroutine(SpawnOnlyRoutine(passenger.GetHandPosition()));
        }
        else if (!passenger.isRightSide)
        {
            currentPoseState = 1;
            Transform handPos = handPosSitL != null ? handPosSitL : passenger.GetHandPosition();
            StartCoroutine(SpawnOnlyRoutine(handPos));
        }
        else
        {
            currentPoseState = 2;
            Transform handPos = handPosSitR != null ? handPosSitR : passenger.GetHandPosition();
            StartCoroutine(SpawnOnlyRoutine(handPos));
        }

        waitingForCollection = true;
    }

    void OpenPaymentUI()
    {
        isTransactionActive = true;
        TogglePaymentUI(true);
        if (textStatus != null) textStatus.text = "";
        UpdateUI();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    IEnumerator SpawnOnlyRoutine(Transform handPos)
    {
        yield return new WaitForSeconds(0.3f);
        if (npcSpawner != null && handPos != null)
            npcSpawner.SpawnMoney(npcPlannedPayment, handPos);
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
        UpdateUI();
    }

    public void SubmitTransaction()
    {
        if (!isTransactionActive) return;

        int correctChangeNeeded = moneyReceived - currentTicket.price;
        int diff = currentChange - correctChangeNeeded;

        if (moneyReceived >= currentTicket.price && diff >= 0)
            isTransactionActive = false;

        StartCoroutine(ShowResultAndClose(diff));
    }

    IEnumerator ShowResultAndClose(int diff)
    {
        if (textStatus == null) yield break;

        if (moneyReceived == 0)
        {
            textStatus.text = "รับเงินจากผู้โดยสารก่อน!";
            textStatus.color = Color.red;
            yield return new WaitForSeconds(1.5f);
            if (isTransactionActive) textStatus.text = "";
            yield break;
        }

        if (moneyReceived < currentTicket.price)
        {
            textStatus.text = "ขาดเงิน " + (currentTicket.price - moneyReceived) + " บาท!";
            textStatus.color = Color.red;
            yield return new WaitForSeconds(2.0f);
            isTransactionActive = true;
            textStatus.text = "";
        }
        else if (diff == 0)
        {
            // ✅ ทอนเป๊ะ ความนิยมบวก 2%
            if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(2f);

            PlayStatusSound(sfxSuccess); // 🌟 เล่นเสียงทอนถูก 🌟
            textStatus.text = successQuotes[Random.Range(0, successQuotes.Length)];
            textStatus.color = Color.green;
            yield return new WaitForSeconds(2.0f);
            CloseTransaction();
        }
        else if (diff < 0)
        {
            // ❌ ทอนขาด โกงผู้โดยสาร โดนด่า ความนิยมตกหนักลบ 10%
            if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(-10f);

            PlayStatusSound(sfxFailUnder); // 🌟 เล่นเสียงทอนขาด (น้อยไป) 🌟
            textStatus.text = failUnderQuotes[Random.Range(0, failUnderQuotes.Length)] + "\n(ขาด " + Mathf.Abs(diff) + ")";
            textStatus.color = Color.red;
            yield return new WaitForSeconds(2.0f);
            isTransactionActive = true;
            textStatus.text = "";
        }
        else
        {
            // 🌟 ทอนเกิน ผู้โดยสารยิ้มหวาน ความนิยมบวก 5% (แต่เราขาดทุนเงินนะ!)
            if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(5f);

            PlayStatusSound(sfxFailOver); // 🌟 เล่นเสียงทอนเกิน 🌟
            textStatus.text = failOverQuotes[Random.Range(0, failOverQuotes.Length)] + "\n(เกิน " + diff + ")";
            textStatus.color = Color.yellow;
            yield return new WaitForSeconds(3.0f);
            CloseTransaction();
        }
    }

    void CloseTransaction()
    {
        ResetTextDisplay();

        int profit = moneyReceived - currentChange;
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.AddMoney(profit);

        if (GameManager.Instance != null) GameManager.Instance.AddDailyIncome(profit);

        moneyReceived = 0;
        currentChange = 0;
        npcPlannedPayment = 0;
        isTransactionActive = false;
        waitingForCollection = false;

        TogglePaymentUI(false);
        AnimateHand(false);

        if (cylinderAnimator != null)
            cylinderAnimator.SetBool(lidBoolName, false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (playerController != null)
            playerController.ResetInteraction();

        if (npcAnimator != null)
        {
            if (currentPoseState == 0) npcAnimator.SetTrigger("trigStandDone");
            else if (currentPoseState == 1) npcAnimator.SetTrigger("trigSitDoneL");
            else npcAnimator.SetTrigger("trigSitDoneR");
        }

        if (currentPassenger != null)
        {
            currentPassenger.PaymentCompleted();
            currentPassenger = null;
        }
    }

    void UpdateUI()
    {
        if (textPrice != null && currentTicket != null)
            textPrice.text = currentTicket.price.ToString() + " ฿";
        if (textReceived != null)
            textReceived.text = moneyReceived.ToString() + " ฿";
        if (textChange != null)
            textChange.text = currentChange.ToString() + " ฿";
    }

    void ResetTextDisplay()
    {
        if (textPrice != null) textPrice.text = "- ฿";
        if (textReceived != null) textReceived.text = "0 ฿";
        if (textChange != null) textChange.text = "0 ฿";
        if (textStatus != null) textStatus.text = "";
    }

    void PlayMoneySound(int amount)
    {
        if (audioSource == null) return;

        // ✅ สำคัญ: ต้องรีเซ็ต Pitch กลับมาใกล้ๆ 1 เพื่อให้เสียงเหรียญ/แบงก์ ใสปิ๊งตามปกติ
        audioSource.pitch = Random.Range(0.95f, 1.05f);

        if (amount <= 10)
        {
            if (sfxCoins != null && sfxCoins.Length > 0)
                audioSource.PlayOneShot(sfxCoins[Random.Range(0, sfxCoins.Length)]);
        }
        else
        {
            if (sfxBanks != null && sfxBanks.Length > 0)
                audioSource.PlayOneShot(sfxBanks[Random.Range(0, sfxBanks.Length)]);
        }
    }

    // 🌟 2. ฟังก์ชันเสริมสำหรับเล่นเสียงสถานะ (พากย์เสียง) 🌟
    void PlayStatusSound(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;

        // ✅ จุดสูตรโกง: กด Pitch ลงมาที่ 0.7 - 0.85 เพื่อบีบเสียงผู้หญิงให้ทุ้มใหญ่เป็นเสียงผู้ชาย/ลุง!
        // (ถ้ายังใหญ่ไม่พอ ลองแก้เป็น 0.6 ดูครับ)
        audioSource.pitch = Random.Range(0.8f, 0.85f);

        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}