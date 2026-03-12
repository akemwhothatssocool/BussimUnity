using UnityEngine;
using System.Collections; // 🌟 สำคัญ! ต้องมีตัวนี้ถึงจะใช้ระบบหน่วงเวลา (IEnumerator) ได้

public class TicketCylinder : MonoBehaviour
{
    [Header("=== ส่วนที่ 1: กระบอกตั๋ว ===")]
    public Animator cylinderAnimator;
    public AudioSource cylinderAudioSource;

    [Header("=== ส่วนที่ 1.5: ไฟล์เสียง (Audio Clip) ===")]
    public AudioClip clickClip; // ⚠️ เอาไฟล์เสียงแก๊ก (.wav/.mp3) มาใส่ช่องนี้
    public AudioClip coinClip;  // ⚠️ เอาไฟล์เสียงเหรียญกริ๊ง (.wav/.mp3) มาใส่ช่องนี้

    [Header("=== ส่วนที่ 2: สายตา ===")]
    public float rayDistance = 2f;
    public GameObject interactUI;
    public Transform playerCamera;
    public LayerMask npcLayer;

    [Header("=== ส่วนที่ 3: ตั้งค่าเสียง ===")]
    [Tooltip("เวลาหน่วงก่อนเสียงเหรียญจะดัง (วินาที) ให้แก๊กนำไปก่อน")]
    public float coinDelay = 0.15f;

    void Update()
    {
        if (Cursor.visible) return;

        CheckLookingAtNPC();

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
        if (cylinderAnimator != null) cylinderAnimator.Play("TicketClick", -1, 0f);

        // 1. เล่นเสียง "แก๊ก" ทันทีที่กดคลิก
        if (cylinderAudioSource != null && clickClip != null)
        {
            cylinderAudioSource.pitch = Random.Range(0.9f, 1.1f);
            cylinderAudioSource.PlayOneShot(clickClip, 1.0f); // ดัง 100%
        }

        // 2. เรียกตัวหน่วงเวลาให้เล่นเสียง "เหรียญ" ตามมาติดๆ
        if (cylinderAudioSource != null && coinClip != null)
        {
            StartCoroutine(PlayCoinDelayed());
        }
    }

    IEnumerator PlayCoinDelayed()
    {
        // หน่วงเวลาเสี้ยววินาที (0.15 วิ) ให้เสียงแก๊กจบ/นำไปก่อน
        yield return new WaitForSeconds(coinDelay);

        // เล่นเสียง "เหรียญกริ๊ง"
        cylinderAudioSource.pitch = Random.Range(0.85f, 1.15f);
        cylinderAudioSource.PlayOneShot(coinClip, 0.6f); // ดรอปเสียงเหรียญลงมาเหลือ 60% จะได้ไม่กลบเสียงแก๊ก
    }
}