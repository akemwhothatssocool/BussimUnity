using UnityEngine;
using System.Collections;

public class TicketCylinder : MonoBehaviour
{
    [Header("=== ส่วนที่ 1: กระบอกตั๋ว ===")]
    public Animator cylinderAnimator;
    public AudioSource cylinderAudioSource;

    [Header("=== ส่วนที่ 1.5: ไฟล์เสียง (Audio Clip) ===")]
    public AudioClip clickClip;
    public AudioClip coinClip;

    [Header("=== ส่วนที่ 2: สายตา (Interaction) ===")]
    public float rayDistance = 2f;
    public GameObject interactUI;
    public Transform playerCamera;
    public LayerMask npcLayer;

    [Header("=== ส่วนที่ 3: ตั้งค่าเสียง (Audio Settings) ===")]
    [Tooltip("เวลาหน่วงก่อนเสียงเหรียญจะดัง (วินาที)")]
    public float coinDelay = 0.15f;

    [Range(0f, 1f)] public float clickVolume = 0.8f; // 🌟 ปรับความดังเสียงแก๊ก
    [Range(0f, 1f)] public float coinVolume = 0.3f;  // 🌟 ปรับความดังเสียงเหรียญ (แนะนำให้เบากว่าแก๊ก)

    void Update()
    {
        // ถ้าเมาส์โผล่ (หน้าจอ UI เปิดอยู่) จะกดคลิกเขย่ากระบอกไม่ได้
        if (Cursor.visible) return;

        CheckLookingAtNPC();

        // คลิกซ้ายเพื่อ "เขย่ากระบอกตั๋ว"
        if (Input.GetButtonDown("Fire1"))
        {
            PlayClickAction();
        }
    }

    void CheckLookingAtNPC()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, npcLayer))
        {
            // ถ้าส่องไปเจอ NPC ให้ขึ้น UI บอกว่ากด E หรือคลิกได้
            if (hit.collider.CompareTag("NPC"))
            {
                if (interactUI != null) interactUI.SetActive(true);
            }
            else
            {
                if (interactUI != null) interactUI.SetActive(false);
            }
        }
        else
        {
            if (interactUI != null) interactUI.SetActive(false);
        }
    }

    void PlayClickAction()
    {
        // เล่น Animation เขย่ากระบอก
        if (cylinderAnimator != null)
            cylinderAnimator.Play("TicketClick", -1, 0f);

        // 1. เล่นเสียง "แก๊ก" ทันที
        if (cylinderAudioSource != null && clickClip != null)
        {
            cylinderAudioSource.pitch = Random.Range(0.9f, 1.1f);
            cylinderAudioSource.PlayOneShot(clickClip, clickVolume);
        }

        // 2. เรียกตัวหน่วงเวลาให้เล่นเสียง "เหรียญ" ตามมา
        if (cylinderAudioSource != null && coinClip != null)
        {
            StartCoroutine(PlayCoinDelayed());
        }
    }

    IEnumerator PlayCoinDelayed()
    {
        // รอเสี้ยววินาทีเพื่อให้เสียงแก๊กนำไปก่อน
        yield return new WaitForSeconds(coinDelay);

        // เล่นเสียง "เหรียญกริ๊ง" ในกระบอก
        if (cylinderAudioSource != null && coinClip != null)
        {
            cylinderAudioSource.pitch = Random.Range(0.85f, 1.15f);
            cylinderAudioSource.PlayOneShot(coinClip, coinVolume);
        }
    }
}