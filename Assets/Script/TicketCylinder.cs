using UnityEngine;
using UnityEngine.UI;
using TMPro; // ใช้ตัวนี้เพราะคุณใช้ TextMeshPro

public class TicketCylinder : MonoBehaviour
{
    [Header("=== ส่วนที่ 1: กระบอกตั๋ว ===")]
    public Animator cylinderAnimator;
    public AudioSource clickSound;

    [Header("=== ส่วนที่ 2: สายตา ===")]
    public float rayDistance = 2f;
    public GameObject interactUI;
    public Transform playerCamera;
    public LayerMask npcLayer; // <--- [เพิ่มบรรทัดนี้] ตัวกรอง Layer

    void Update()
    {
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

        // [แก้บรรทัดนี้] เพิ่ม npcLayer เข้าไปท้ายสุด
        // แปลว่า: ยิงแสงออกไปนะ แต่ชนแค่ของที่อยู่ใน Layer ที่ระบุเท่านั้น!
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