using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TicketCylinder : MonoBehaviour
{
    [Header("=== ส่วนที่ 1: กระบอกตั๋ว ===")]
    public Animator cylinderAnimator;
    public AudioSource clickSound;

    [Header("=== ส่วนที่ 2: สายตา ===")]
    public float rayDistance = 2f;
    public GameObject interactUI;
    public Transform playerCamera;
    public LayerMask npcLayer;

    void Update()
    {
        // 🛑 เพิ่มบรรทัดนี้: ถ้าเมาส์โชว์อยู่ (หน้าจอคิดเงินเปิดอยู่) ให้หยุดการทำงานทั้งหมดในนี้!
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
        if (clickSound != null)
        {
            clickSound.pitch = Random.Range(0.9f, 1.1f);
            clickSound.PlayOneShot(clickSound.clip);
        }
    }
}