using UnityEngine;
using TMPro;

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

    [Header("Layout")]
    [SerializeField] private Vector2 lowerScreenOffset = new Vector2(0f, -180f);

    [Header("Key Display")]
    [SerializeField] private string interactKey = "E";
    [SerializeField] private int spriteIndex = 0; // ใช้ index แทน
    [SerializeField] private bool useSprite = true;

    private bool isVisible = false;
    private float targetAlpha = 0f;
    private Vector3 originalScale;
    private float scaleTimer = 0f;
    private RectTransform rectTransform;
    private Vector2 defaultAnchoredPosition;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = transform as RectTransform;
        if (rectTransform != null)
            defaultAnchoredPosition = rectTransform.anchoredPosition;

        originalScale = transform.localScale;
        Hide(true);
    }

    private void Start()
    {
        if (keyText != null && useSprite)
        {
            keyText.text = $"<sprite={spriteIndex}>";
        }
        else if (keyText != null)
        {
            keyText.text = interactKey;
        }
    }

    private void Update()
    {
        UpdateFade();
        UpdatePulseAnimation();
    }

    public void Show(string message)
    {
        Show(message, false);
    }

    public void Show(string message, bool useLowerPosition)
    {
        if (!isVisible)
        {
            isVisible = true;
            targetAlpha = 1f;
        }

        if (promptText != null)
        {
            if (useSprite)
            {
                // ใช้ index ง่ายกว่า
                message = message.Replace("E", $"<sprite={spriteIndex}>");
            }
            promptText.text = message;
        }

        ApplyPromptPosition(useLowerPosition);
    }

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

        ApplyPromptPosition(false);
    }

    public bool IsVisible => isVisible;

    private void UpdateFade()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

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

        scaleTimer += Time.deltaTime * scaleSpeed;
        float scale = 1f + Mathf.Sin(scaleTimer) * (scaleAmount - 1f) * 0.5f;
        transform.localScale = originalScale * scale;
    }

    private void ApplyPromptPosition(bool useLowerPosition)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = useLowerPosition
            ? defaultAnchoredPosition + lowerScreenOffset
            : defaultAnchoredPosition;
    }
}
