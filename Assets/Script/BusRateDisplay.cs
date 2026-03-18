using UnityEngine;
using UnityEngine.UI;

public class BusRateDisplay : MonoBehaviour
{
    [Header("Star Sprites")]
    public Sprite fullStarSprite;
    public Sprite halfStarSprite;

    [Header("UI Star Images")]
    public Image[] starImages;

    public void UpdateBusRate(float busRate)
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            if (busRate >= i + 1f)
            {
                starImages[i].sprite = fullStarSprite;
                starImages[i].enabled = true;  // โชว์ดาวเต็ม
            }
            else if (busRate >= i + 0.5f)
            {
                starImages[i].sprite = halfStarSprite;
                starImages[i].enabled = true;  // โชว์ดาวครึ่ง
            }
            else
            {
                starImages[i].enabled = false; // ซ่อน → เห็นพื้นหลัง Ratebar แทน
            }
        }
    }

    // --- ส่วนทดสอบ ---
    [Header("Test Bus Rate")]
    [Range(0, 5)]
    public float testBusRate = 3.5f;

    [SerializeField] private bool testMode = false;

    void Update()
    {
        if (testMode)
        {
            UpdateBusRate(testBusRate);
        }
    }
}