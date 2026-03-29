using System.Collections;
using UnityEngine;
using TMPro;

public class FareSystem : MonoBehaviour
{
    // ... (ส่วนประกาศตัวแปร Header 1 ถึง 8 ปล่อยไว้เหมือนเดิม ไม่ต้องแก้ครับ) ...
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
    public int baseFare = 10;
    public int farePerStop = 5;
    public TicketData currentTicket;
    public int moneyReceived;
    public int currentChange;
    private bool isTransactionActive = false;
    private bool waitingForCollection = false;
    private int npcPlannedPayment = 0;
    private bool isProcessingSubmit = false;

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
    [Range(0f, 1f)] public float moneyVolume = 0.3f;
    [Range(0f, 1f)] public float statusVolume = 1.0f;

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
        StopAllCoroutines();

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

        // ==================================================
        // 🌟 ระบบคำนวณราคาแบบใหม่ (อิงจากระยะทางป้าย)
        // ==================================================
        int currentStop = 0;
        int maxStops = 6;
        if (GameManager.Instance != null)
        {
            currentStop = GameManager.Instance.stopsReached;
            maxStops = GameManager.Instance.stopsPerDay;
        }

        // 1. เช็กว่าเหลืออีกกี่ป้ายถึงจะสุดสาย
        int remainingStops = maxStops - currentStop;
        if (remainingStops < 1) remainingStops = 1; // กันบั๊กคนขึ้นป้ายสุดท้าย

        // 2. สุ่มว่าคนนี้จะนั่งกี่ป้าย (1 ป้าย ไปจนถึงสุดสาย)
        int travelStops = Random.Range(1, remainingStops + 1);

        // 3. คำนวณราคา: ค่าโดยสารเริ่มต้น + (จำนวนป้าย * 5 บาท)
        // เช่น นั่ง 2 ป้าย = 10 + (2 * 5) = 20 บาท
        currentTicket.price = baseFare + (travelStops * farePerStop);

        // 4. ส่งข้อมูลเป้าหมายไปเก็บไว้ที่ตัวผู้โดยสาร (เผื่อเอาไปใช้สั่งให้ลงรถ)
        passenger.targetStop = currentStop + travelStops;
        // ==================================================

        // สุ่มแบงก์/เหรียญที่ NPC จะจ่าย โดยต้องเป็นยอดที่ผู้เล่นทอนได้จริง
        npcPlannedPayment = GenerateRealisticPayment(currentTicket.price, GetAvailableWalletMoney());

        moneyReceived = 0;
        currentChange = 0;
        AnimateHand(true);

        if (cylinderAnimator != null) cylinderAnimator.SetBool(lidBoolName, true);

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

        int availableWallet = GetAvailableWalletMoney();
        if (currentChange + amount > availableWallet)
        {
            if (textStatus != null)
            {
                textStatus.text = "เงินทอนเกินเงินที่มี!";
                textStatus.color = Color.red;
            }
            return;
        }

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
    // 🌟 ดักไว้ไม่ให้กดยืนยันรัวๆ ตอนที่กำลังประมวลผลข้อความอยู่
    if (!isTransactionActive || isProcessingSubmit) return; 

    int correctChange = moneyReceived - currentTicket.price;
    int diff = currentChange - correctChange;

    if (moneyReceived >= currentTicket.price && diff >= 0) 
        isTransactionActive = false;

    StartCoroutine(ShowResultAndClose(diff));
}

IEnumerator ShowResultAndClose(int diff)
{
    isProcessingSubmit = true; // 🔒 ล็อกการกดยืนยันชั่วคราว

    int correctChange = moneyReceived - currentTicket.price;

    if (moneyReceived == 0)
    {
        textStatus.text = "รับเงินก่อน!";
        textStatus.color = Color.red;
        yield return new WaitForSeconds(1.2f);
        isProcessingSubmit = false; // 🔓 ปลดล็อกให้กดใหม่ได้
        yield break;
    }

    if (moneyReceived < currentTicket.price)
    {
        textStatus.text = "เงินไม่พอ!";
        yield return new WaitForSeconds(1.2f);
        isProcessingSubmit = false; // 🔓 ปลดล็อกให้รับเงินเพิ่มได้
    }
    else if (correctChange > GetAvailableWalletMoney())
    {
        if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(-2f);

        textStatus.text = "เงินทอนไม่พอ ขอแบงก์ย่อย!";
        textStatus.color = Color.red;
        yield return new WaitForSeconds(1.5f);

        RequestSmallerPayment();
        isProcessingSubmit = false;
    }
    else if (diff == 0)
    {
        if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(2f);
        PlayStatusSound(sfxSuccess);
        textStatus.text = successQuotes[Random.Range(0, successQuotes.Length)];
        textStatus.color = Color.green;
        yield return new WaitForSeconds(1.8f);
        isProcessingSubmit = false;
        CloseTransaction();
    }
    else if (diff < 0)
    {
        if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(-10f);
        PlayStatusSound(sfxFailUnder);
        textStatus.text = failUnderQuotes[Random.Range(0, failUnderQuotes.Length)];
        textStatus.color = Color.red;
        yield return new WaitForSeconds(1.8f);
        isProcessingSubmit = false; // 🔓 ปลดล็อกให้ทอนเงินเพิ่มได้
    }
    else
    {
        if (GameManager.Instance != null) GameManager.Instance.AdjustPopularity(5f);
        PlayStatusSound(sfxFailOver);
        textStatus.text = failOverQuotes[Random.Range(0, failOverQuotes.Length)];
        textStatus.color = Color.yellow;
        yield return new WaitForSeconds(2.5f);
        isProcessingSubmit = false;
        CloseTransaction();
    }
}

    void CloseTransaction()
    {
        int profit = moneyReceived - currentChange;
        int tipBonus = currentPassenger != null ? currentPassenger.GetSeatTipBonus() : 0;
        int totalIncome = profit + tipBonus;

        if (PlayerWallet.Instance != null) PlayerWallet.Instance.AddMoney(totalIncome);
        if (GameManager.Instance != null) GameManager.Instance.AddDailyIncome(totalIncome);

        // 🌟 1. อัปเดตสถานะ NPC และสั่งให้รู้ว่าจ่ายเงินเสร็จแล้ว
        if (currentPassenger != null)
        {
            currentPassenger.hasPaidTicket = true; // ✅ เพิ่มบรรทัดนี้ตามที่ต้องการ
            currentPassenger.PaymentCompleted();
        }

        // 🌟 2. สั่งเล่นแอนิเมชันเก็บมือ
        if (npcAnimator != null)
        {
            string trig = (currentPoseState == 0) ? "trigStandDone" : (currentPoseState == 1 ? "trigSitDoneL" : "trigSitDoneR");
            npcAnimator.SetTrigger(trig);
        }

        // 🌟 3. ล้างความจำและเคลียร์ระบบทั้งหมด
        ForceResetSystem();
    }

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
        audioSource.pitch = Random.Range(0.8f, 0.85f);
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], statusVolume);
    }

    void UpdateUI()
    {
        if (textPrice != null) textPrice.text = currentTicket.price + " ฿";
        if (textReceived != null) textReceived.text = moneyReceived + " ฿";
        if (textChange != null) textChange.text = currentChange + " ฿";
        UpdateTransactionHelperText();
    }

    void ResetTextDisplay()
    {
        if (textPrice != null) textPrice.text = "- ฿";
        if (textReceived != null) textReceived.text = "0 ฿";
        if (textChange != null) textChange.text = "0 ฿";
        if (textStatus != null) textStatus.text = "";
    }

    void TogglePaymentUI(bool v) { if (paymentUIElements != null) foreach (var ui in paymentUIElements) if (ui != null) ui.SetActive(v); }

    int GetAvailableWalletMoney()
    {
        return PlayerWallet.Instance != null ? Mathf.Max(0, PlayerWallet.Instance.currentMoney) : 0;
    }

    void UpdateTransactionHelperText()
    {
        if (textStatus == null || !isTransactionActive || isProcessingSubmit) return;

        int availableWallet = GetAvailableWalletMoney();
        int remainingAfterSelection = Mathf.Max(0, availableWallet - currentChange);
        textStatus.text = $"เงินสดสำหรับทอน: {availableWallet} ฿ | เหลือหลังเลือก: {remainingAfterSelection} ฿";
        textStatus.color = Color.white;
    }

    Transform GetCurrentMoneyHand()
    {
        if (currentPassenger == null) return null;

        if (!currentPassenger.isSittingSeat) return currentPassenger.GetHandPosition();
        if (!currentPassenger.isRightSide) return handPosSitL != null ? handPosSitL : currentPassenger.GetHandPosition();
        return handPosSitR != null ? handPosSitR : currentPassenger.GetHandPosition();
    }

    void RequestSmallerPayment()
    {
        moneyReceived = 0;
        currentChange = 0;
        isTransactionActive = false;
        waitingForCollection = true;

        TogglePaymentUI(false);
        ResetTextDisplay();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        npcPlannedPayment = GenerateRealisticPayment(currentTicket.price, GetAvailableWalletMoney());
        Transform hand = GetCurrentMoneyHand();
        if (hand != null) StartCoroutine(SpawnOnlyRoutine(hand));
    }

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

    // 🌟 ฟังก์ชันใหม่: สุ่มเงินจ่ายแบบอิงความน่าจะเป็น (Weighted Random)
    int GenerateRealisticPayment(int price, int availableChange)
    {
        int maxPayableAmount = price + Mathf.Max(0, availableChange);

        // ถ้าทอนได้น้อยมาก บังคับให้จ่ายพอดีเพื่อไม่ให้เกมค้าง
        if (maxPayableAmount <= price)
        {
            return price;
        }

        // สุ่มตัวเลข 1-100 เพื่อเช็กดวงของผู้โดยสารคนนี้
        int chance = Random.Range(1, 101);

        // โอกาส 40% จ่ายพอดีเป๊ะ (หรือเศษเหรียญใกล้เคียง)
        if (chance <= 40)
        {
            return price;
        }
        // โอกาส 35% จ่ายแบงก์ 20 (ถ้าค่าตั๋วไม่เกิน 20)
        else if (chance <= 75 && price <= 20 && 20 <= maxPayableAmount)
        {
            return 20;
        }
        // โอกาส 15% จ่ายแบงก์ 50 (ถ้าค่าตั๋วไม่เกิน 50)
        else if (chance <= 90 && price <= 50 && 50 <= maxPayableAmount)
        {
            return 50;
        }
        // โอกาส 7% จ่ายแบงก์ 100
        else if (chance <= 97 && price <= 100 && 100 <= maxPayableAmount)
        {
            return 100;
        }
        // โอกาส 2% จ่ายแบงก์ 500
        else if (chance <= 99 && 500 <= maxPayableAmount)
        {
            return 500;
        }
        // โอกาส 1% แจ็กพอต จ่ายแบงก์ 1000 (ผู้โดยสารรวยจัด)
        else if (1000 <= maxPayableAmount)
        {
            return 1000;
        }
        // ถ้าแบงก์ใหญ๋เกินกว่าที่ผู้เล่นจะทอนได้ ให้ไล่ลงมาที่ยอดที่ปลอดภัยที่สุด
        else if (100 <= maxPayableAmount)
        {
            return 100;
        }
        else if (50 <= maxPayableAmount)
        {
            return 50;
        }
        else if (20 <= maxPayableAmount)
        {
            return 20;
        }
        else
        {
            return price;
        }
    }
}
