using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassengerThoughtBubble : MonoBehaviour
{
    [SerializeField] Vector3 localOffset = new Vector3(0f, 1.72f, 0f);
    [SerializeField] float worldScale = 0.0078f;
    [SerializeField] float maxWidth = 210f;
    [SerializeField] Vector2 padding = new Vector2(22f, 14f);
    [SerializeField] float fadeDuration = 0.18f;

    RectTransform bubbleRoot;
    RectTransform panelRect;
    RectTransform textRect;
    CanvasGroup canvasGroup;
    TextMeshProUGUI bubbleLabel;
    Coroutine visibilityRoutine;

    void Awake()
    {
        EnsureBubble();
    }

    public void Show(string message, float duration = 2.8f)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        EnsureBubble();

        if (visibilityRoutine != null)
            StopCoroutine(visibilityRoutine);

        bubbleLabel.text = message;
        bubbleLabel.ForceMeshUpdate();

        float availableWidth = Mathf.Max(104f, maxWidth - padding.x);
        Vector2 preferred = bubbleLabel.GetPreferredValues(message, availableWidth, 0f);
        float bubbleWidth = Mathf.Clamp(preferred.x + padding.x, 118f, maxWidth);
        float bubbleHeight = Mathf.Max(44f, preferred.y + padding.y);

        bubbleRoot.sizeDelta = new Vector2(bubbleWidth, bubbleHeight + 18f);
        panelRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

        bubbleRoot.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        visibilityRoutine = StartCoroutine(HideAfterDelay(duration));
    }

    public void HideImmediate()
    {
        if (visibilityRoutine != null)
            StopCoroutine(visibilityRoutine);

        visibilityRoutine = null;

        if (bubbleRoot != null)
            bubbleRoot.gameObject.SetActive(false);
    }

    IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        bubbleRoot.gameObject.SetActive(false);
        visibilityRoutine = null;
    }

    void EnsureBubble()
    {
        if (bubbleRoot != null)
            return;

        GameObject root = new GameObject("ThoughtBubble", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(Billboard));
        root.layer = gameObject.layer;

        bubbleRoot = root.GetComponent<RectTransform>();
        bubbleRoot.SetParent(transform, false);
        bubbleRoot.localPosition = localOffset;
        bubbleRoot.localRotation = Quaternion.identity;
        bubbleRoot.localScale = Vector3.one * worldScale;
        bubbleRoot.sizeDelta = new Vector2(maxWidth, 74f);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 26f;

        canvasGroup = root.GetComponent<CanvasGroup>();

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = root.layer;
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(bubbleRoot, false);
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        panelImage.raycastTarget = false;

        CreateTailDot(panelRect, "TailDotLarge", new Vector2(-28f, -10f), new Vector2(14f, 14f), new Color(0.08f, 0.08f, 0.08f, 0.86f));
        CreateTailDot(panelRect, "TailDotSmall", new Vector2(-40f, -20f), new Vector2(8f, 8f), new Color(0.08f, 0.08f, 0.08f, 0.72f));

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = root.layer;
        textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(panelRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(padding.x * 0.5f, padding.y * 0.5f);
        textRect.offsetMax = new Vector2(-padding.x * 0.5f, -padding.y * 0.5f);

        bubbleLabel = textObject.GetComponent<TextMeshProUGUI>();
        bubbleLabel.font = TMP_Settings.defaultFontAsset;
        bubbleLabel.fontSize = 18f;
        bubbleLabel.color = new Color(1f, 0.98f, 0.94f, 1f);
        bubbleLabel.alignment = TextAlignmentOptions.Center;
        bubbleLabel.textWrappingMode = TextWrappingModes.Normal;
        bubbleLabel.overflowMode = TextOverflowModes.Overflow;
        bubbleLabel.raycastTarget = false;

        bubbleRoot.gameObject.SetActive(false);
    }

    void CreateTailDot(RectTransform parent, string objectName, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject dotObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dotObject.layer = parent.gameObject.layer;

        RectTransform dotRect = dotObject.GetComponent<RectTransform>();
        dotRect.SetParent(parent, false);
        dotRect.anchorMin = new Vector2(0.5f, 0f);
        dotRect.anchorMax = new Vector2(0.5f, 0f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = anchoredPosition;
        dotRect.sizeDelta = sizeDelta;

        Image dotImage = dotObject.GetComponent<Image>();
        dotImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        dotImage.type = Image.Type.Simple;
        dotImage.color = color;
        dotImage.raycastTarget = false;
    }
}
