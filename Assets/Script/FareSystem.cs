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

    // ==========================================
    // ✅ เปลี่ยนเป็นรับค่า Animator แทน
    // ==========================================
    [Header("แอนิเมชันกระบอกตั๋ว (มือซ้าย)")]
    public Animator cylinderAnimator; // ลาก GameObject ที่มี Animator ของกระบอกตั๋วมาใส่
    public string lidBoolName = "IsOpen"; // ชื่อ Parameter แบบ Bool ใน Animator ของคุณ
    // ==========================================

    [Header("หน้าจอ UI ที่ต้องการให้ซ่อน/แสดงตอนคิดเงิน")]
    public GameObject[] paymentUIElements;

    [Header("ตัวเลขในแคปซูล (โชว์แค่ตัวเลข)")]
    public TextMeshProUGUI textPrice;
    public TextMeshProUGUI textReceived;
    public TextMeshProUGUI textChange;

    [Header("ข้อความแจ้งเตือน / คำพูด NPC")]
    public TextMeshProUGUI textStatus;

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

    void TogglePaymentUI(bool isVisible)
    {
        if (paymentUIElements == null || paymentUIElements.Length == 0) return;

        foreach (GameObject ui in paymentUIElements)
        {
            if (ui != null) ui.SetActive(isVisible);
        }
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
        currentTicket.price = 20;

        moneyReceived = 0;
        currentChange = 0;
        isTransactionActive = true;

        AnimateHand(true);

        // ✅ สั่งเปิดฝากระบอกตั๋วผ่าน Animator (เซ็ต Bool ให้เป็น True)
        if (cylinderAnimator != null)
        {
            cylinderAnimator.SetBool(lidBoolName, true);
        }

        if (!passenger.isSittingSeat)
        {
            currentPoseState = 0;
            StartCoroutine(StandThenPayRoutine());
        }
        else if (!passenger.isRightSide)
        {
            currentPoseState = 1;
            StartCoroutine(SitThenPayRoutine("trigSitGiveL", handPosSitL));
        }
        else
        {
            currentPoseState = 2;
            StartCoroutine(SitThenPayRoutine("trigSitGiveR", handPosSitR));
        }

        TogglePaymentUI(true);
        if (textStatus != null) textStatus.text = "";

        UpdateUI();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ... (ส่วน Routine ยื่นเงิน NPC เหมือนเดิม) ...
    IEnumerator StandThenPayRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        if (npcAnimator != null) npcAnimator.SetTrigger("trigStandGive");
        yield return new WaitForSeconds(0.5f);

        if (npcSpawner != null && currentPassenger != null)
        {
            Transform handPos = currentPassenger.GetHandPosition();
            if (handPos != null) npcSpawner.SpawnMoney(currentTicket.price, handPos);
        }
    }

    IEnumerator SitThenPayRoutine(string giveTriggerName, Transform handPosOverride)
    {
        yield return new WaitForSeconds(0.1f);
        if (npcAnimator != null) npcAnimator.SetTrigger(giveTriggerName);
        yield return new WaitForSeconds(0.5f);

        if (npcSpawner != null && currentPassenger != null)
        {
            Transform handPos = handPosOverride != null ? handPosOverride : currentPassenger.GetHandPosition();
            if (handPos != null) npcSpawner.SpawnMoney(currentTicket.price, handPos);
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
            textStatus.text = successQuotes[Random.Range(0, successQuotes.Length)];
            textStatus.color = Color.green;
            yield return new WaitForSeconds(2.0f);
            CloseTransaction();
        }
        else if (diff < 0)
        {
            textStatus.text = failUnderQuotes[Random.Range(0, failUnderQuotes.Length)] + "\n(ขาด " + Mathf.Abs(diff) + ")";
            textStatus.color = Color.red;
            yield return new WaitForSeconds(2.0f);
            isTransactionActive = true;
            textStatus.text = "";
        }
        else
        {
            textStatus.text = failOverQuotes[Random.Range(0, failOverQuotes.Length)] + "\n(เกิน " + diff + ")";
            textStatus.color = Color.yellow;
            yield return new WaitForSeconds(3.0f);
            CloseTransaction();
        }
    }

    void CloseTransaction()
    {
        ResetTextDisplay();

        moneyReceived = 0;
        currentChange = 0;
        isTransactionActive = false;

        TogglePaymentUI(false);
        AnimateHand(false);

        // ✅ สั่งปิดฝากระบอกตั๋ว (เซ็ต Bool กลับเป็น False)
        if (cylinderAnimator != null)
        {
            cylinderAnimator.SetBool(lidBoolName, false);
        }

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
        if (textPrice != null && currentTicket != null) textPrice.text = currentTicket.price.ToString();
        if (textReceived != null) textReceived.text = moneyReceived.ToString();
        if (textChange != null) textChange.text = currentChange.ToString();
    }

    void ResetTextDisplay()
    {
        if (textPrice != null) textPrice.text = "-";
        if (textReceived != null) textReceived.text = "0";
        if (textChange != null) textChange.text = "0";
        if (textStatus != null) textStatus.text = "";
    }

    void PlayMoneySound(int amount)
    {
        if (audioSource == null) return;
        if (amount <= 10) audioSource.PlayOneShot(sfxCoin);
        else audioSource.PlayOneShot(sfxBank);
    }
}