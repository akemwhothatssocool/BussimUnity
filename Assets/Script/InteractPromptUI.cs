using UnityEngine;
using TMPro;

/// <summary>
/// จัดการ Interact Prompt UI แบบสวยงาม
/// รองรับ fade in/out animation
/// </summary>
public class InteractPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI keyText;

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float scaleAmount = 1.1f;
    [SerializeField] private float scaleSpeed = 3f;

    [Header("Key Display")]
    [SerializeField] private string interactKey = "E";

    private bool isVisible = false;
    private float targetAlpha = 0f;
    private Vector3 originalScale;
    private float scaleTimer = 0f;

    private void Awake()
    {
        // Auto-assign components
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (promptText == null)
            promptText = GetComponentInChildren<TextMeshProUGUI>();

        originalScale = transform.localScale;

        // เริ่มต้นซ่อน UI
        Hide(true);
    }

    private void Start()
    {
        // แสดงปุ่ม
        if (keyText != null)
            keyText.text = interactKey;
    }

    private void Update()
    {
        UpdateFade();
        UpdatePulseAnimation();
    }

    #region Public Methods

    /// <summary>
    /// แสดง Prompt พร้อมข้อความ
    /// </summary>
    public void Show(string message)
    {
        if (!isVisible)
        {
            isVisible = true;
            targetAlpha = 1f;

            if (promptText != null)
                promptText.text = message;
        }
    }

    /// <summary>
    /// ซ่อน Prompt
    /// </summary>
    public void Hide(bool immediate = false)
    {
        isVisible = false;
        targetAlpha = 0f;

        if (immediate && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// เช็คว่ากำลังแสดงอยู่หรือไม่
    /// </summary>
    public bool IsVisible => isVisible;

    #endregion

    #region Private Methods

    private void UpdateFade()
    {
        if (canvasGroup == null) return;

        // Smooth fade
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

        // Update interactivity
        if (canvasGroup.alpha > 0.9f)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else if (canvasGroup.alpha < 0.1f)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void UpdatePulseAnimation()
    {
        if (!isVisible) return;

        // Pulse effect
        scaleTimer += Time.deltaTime * scaleSpeed;
        float scale = 1f + Mathf.Sin(scaleTimer) * (scaleAmount - 1f) * 0.5f;
        transform.localScale = originalScale * scale;
    }

    #endregion
}
