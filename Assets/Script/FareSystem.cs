using System.Collections;
using UnityEngine;
using TMPro;

public class FareSystem : MonoBehaviour
{
    [Header("=== 1. ระบบโมเดลและแอนิเมชัน ===")]
    public GameObject rightHandMoneyModel;
    public float handAnimDuration = 0.3f;
    public Vector3 hiddenOffset = new Vector3(0f, -0.8f, 0f);
    private Vector3 visibleHandPos;
    private Vector3 hiddenHandPos;
    private Coroutine handCoroutine;

    [Header("=== 2. กระบอกตั๋ว (มือซ้าย) ===")]
    public Animator cylinderAnimator;
    public string lidBoolName = "IsOpen";

    [Header("=== 3. ระบบ UI แคปซูล ===")]
    public GameObject[] paymentUIElements;
    public TextMeshProUGUI textPrice;
    public TextMeshProUGUI textReceived;
    public TextMeshProUGUI textChange;
    public TextMeshProUGUI textStatus;

    [Header("=== 4. ตั้งค่าระบบคิดเงิน ===")]
    public int[] possiblePrices = { 8, 10, 12, 15, 20 };
    public TicketData currentTicket;
    public int moneyReceived;
    public int currentChange;
    private bool isTransactionActive = false;
    private bool waitingForCollection = false;
    private int npcPlannedPayment = 0;

    [Header("=== 5. ข้อมูล NPC & Player ===")]
    public PassengerAI currentPassenger;
    private int currentPoseState = 0;
    public NPCSpawner npcSpawner;
    public Animator npcAnimator;
    public Transform handPosSitL;
    public Transform handPosSitR;
    public BusPlayerController playerController;

    [Header("=== 6. ระบบเสียง (Audio) ===")]
    public AudioSource audioSource;
    public AudioClip[] sfxCoins;
    public AudioClip[] sfxBanks;
    public AudioClip[] sfxSuccess;
    public AudioClip[] sfxFailUnder;
    public AudioClip[] sfxFailOver;

    [Header("=== 7. ระดับเสียง (Volume Settings) ===")]
    [Range(0f, 1f)] public float moneyVolume = 0.3f;   // เสียงเหรียญ/แบงก์
    [Range(0f, 1f)] public float statusVolume = 1.0f;  // เสียงลุง/NPC

    [Header("=== 8. คลังคำพูด NPC ===")]
    public string[] successQuotes = { "ขอบคุณครับ", "เชิญข้างในเลยครับ" };
    public string[] failUnderQuotes = { "เฮ้ย! ทอนไม่ครบป่าวพี่!!", "จะโกงเหรอ!" };
    public string[] failOverQuotes = { "โอ้โห หวานเจี๊ยบ!", "ขอบคุณเสี่ย!" };

    // ==========================================
    // Lifecycle Methods
    // ==========================================
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

        // ปุ่มยืนยัน (Space / Enter)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            SubmitTransaction();

        // ปุ่มล้างเงินทอน (C / Backspace)
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Backspace))
            ClearChange();
    }

    // ==========================================
    // 🌟 ระบบ Reset (แก้บั๊กข้ามวัน)
    // ==========================================
    public void ForceResetSystem()
    {
        StopAllCoroutines(); // หยุดการทำงานค้างทั้งหมด

        isTransactionActive = false;
        waitingForCollection = false;
        currentPassenger = null;
        moneyReceived = 0;
        currentChange = 0;
        npcPlannedPayment = 0;

        TogglePaymentUI(false);
        AnimateHand(false);
        ResetTextDisplay();

        if (cylinderAnimator != null) cylinderAnimator.SetBool(lidBoolName, false);
        if (playerController != null) playerController.ResetInteraction();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("<color=cyan>FareSystem: Hard Reset Success!</color>");
    }

    // ==========================================
    // Transaction Logic
    // ==========================================
    public void StartTransaction(PassengerAI passenger)
    {
        currentPassenger = passenger;
        npcAnimator = passenger.animator;

        if (currentTicket == null) currentTicket = ScriptableObject.CreateInstance<TicketData>();
        currentTicket.price = possiblePrices[Random.Range(0, possiblePrices.Length)];
        npcPlannedPayment = GenerateRealisticPayment(currentTicket.price);

        moneyReceived = 0;
        currentChange = 0;
        AnimateHand(true);

        if (cylinderAnimator != null) cylinderAnimator.SetBool(lidBoolName, true);

        // เช็คตำแหน่งมือตามท่าทาง
        if (!passenger.isSittingSeat) { currentPoseState = 0; StartCoroutine(SpawnOnlyRoutine(passenger.GetHandPosition())); }
        else if (!passenger.isRightSide) { currentPoseState = 1; StartCoroutine(SpawnOnlyRoutine(handPosSitL != null ? handPosSitL : passenger.GetHandPosition())); }
        else { currentPoseState = 2; StartCoroutine(SpawnOnlyRoutine(handPosSitR != null ? handPosSitR : passenger.GetHandPosition())); }

        waitingForCollection = true;
    }

    void OpenPaymentUI()
    {
        isTransactionActive = true;
        TogglePaymentUI(true);
        UpdateUI();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ReceiveNPCMoney(int amount)
    {
        moneyReceived += amount;
        PlayMoneySound(amount);
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
        int correctChange = moneyReceived - currentTicket.price;
        int diff = currentChange - correctChange;

        // ถ้าจ่ายเงินครบและไม่ทอนขาด ให้จบ Transaction
        if (moneyReceived >= currentTicket.price && diff >= 0) isTransactionActive = false;
        StartCoroutine(ShowResultAndClose(diff));
    }

    IEnumerator ShowResultAndClose(int diff)
    {
        if (moneyReceived == 0)
        {
            textStatus.text = "รับเงินก่อน!";
            textStatus.color = Color.red;
            yield return new WaitForSeconds(1.2f);
            yield break;
        }

        if (moneyReceived < currentTicket.price)
        {
            textStatus.text = "เงินไม่พอ!";
            yield return new WaitForSeconds(1.2f);
        }
        else if (diff == 0)
        {
            if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(2f);
            PlayStatusSound(sfxSuccess);
            textStatus.text = successQuotes[Random.Range(0, successQuotes.Length)];
            textStatus.color = Color.green;
            yield return new WaitForSeconds(1.8f);
            CloseTransaction();
        }
        else if (diff < 0)
        {
            if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(-10f);
            PlayStatusSound(sfxFailUnder);
            textStatus.text = failUnderQuotes[Random.Range(0, failUnderQuotes.Length)];
            textStatus.color = Color.red;
            yield return new WaitForSeconds(1.8f);
        }
        else
        {
            if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(5f);
            PlayStatusSound(sfxFailOver);
            textStatus.text = failOverQuotes[Random.Range(0, failOverQuotes.Length)];
            textStatus.color = Color.yellow;
            yield return new WaitForSeconds(2.5f);
            CloseTransaction();
        }
    }

    void CloseTransaction()
    {
        int profit = moneyReceived - currentChange;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.AddMoney(profit);
        if (GameManager.Instance != null) GameManager.Instance.AddDailyIncome(profit);

        ForceResetSystem(); // ใช้ Hard Reset เพื่อปิดทุกอย่างให้ชัวร์

        if (npcAnimator != null)
        {
            string trig = (currentPoseState == 0) ? "trigStandDone" : (currentPoseState == 1 ? "trigSitDoneL" : "trigSitDoneR");
            npcAnimator.SetTrigger(trig);
        }

        if (currentPassenger != null)
        {
            currentPassenger.PaymentCompleted();
        }
    }

    // ==========================================
    // Helpers & Audio
    // ==========================================
    void PlayMoneySound(int amount)
    {
        if (audioSource == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        if (amount <= 10) audioSource.PlayOneShot(sfxCoins[Random.Range(0, sfxCoins.Length)], moneyVolume);
        else audioSource.PlayOneShot(sfxBanks[Random.Range(0, sfxBanks.Length)], moneyVolume);
    }

    void PlayStatusSound(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;
        audioSource.pitch = Random.Range(0.8f, 0.85f); // เสียงลุงทุ้มๆ
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], statusVolume);
    }

    void UpdateUI()
    {
        if (textPrice != null) textPrice.text = currentTicket.price + " ฿";
        if (textReceived != null) textReceived.text = moneyReceived + " ฿";
        if (textChange != null) textChange.text = currentChange + " ฿";
    }

    void ResetTextDisplay()
    {
        if (textPrice != null) textPrice.text = "- ฿";
        if (textReceived != null) textReceived.text = "0 ฿";
        if (textChange != null) textChange.text = "0 ฿";
        if (textStatus != null) textStatus.text = "";
    }

    void TogglePaymentUI(bool v) { if (paymentUIElements != null) foreach (var ui in paymentUIElements) if (ui != null) ui.SetActive(v); }

    public void AnimateHand(bool show)
    {
        if (rightHandMoneyModel == null) return;
        if (handCoroutine != null) StopCoroutine(handCoroutine);
        handCoroutine = StartCoroutine(HandSlideRoutine(show));
    }

    IEnumerator HandSlideRoutine(bool show)
    {
        Vector3 targetPos = show ? visibleHandPos : hiddenHandPos;
        float elapsed = 0;
        Vector3 startPos = rightHandMoneyModel.transform.localPosition;
        while (elapsed < handAnimDuration)
        {
            rightHandMoneyModel.transform.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / handAnimDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rightHandMoneyModel.transform.localPosition = targetPos;
    }

    IEnumerator SpawnOnlyRoutine(Transform h)
    {
        yield return new WaitForSeconds(0.3f);
        if (npcSpawner != null && h != null) npcSpawner.SpawnMoney(npcPlannedPayment, h);
    }

    int GenerateRealisticPayment(int price)
    {
        int[] bills = { 1000, 500, 100, 50, 20 };
        foreach (int bill in bills) if (bill >= price) return bill;
        return price;
    }
}