using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BackgroundPath = "Assets/Mainmenu Story/Gemini_Generated_Image_ih5thmih5thmih5t.png";
    private const string TitleFontPath = "Assets/Palanquin_Dark/PalanquinDark-Bold SDF.asset";
    private const string MenuFontPath = "Assets/Prompt/Prompt SDF.asset";

    public static void BuildMainMenuScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvas = canvasGo.GetComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.sizeDelta = Vector2.zero;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.localScale = Vector3.one;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        MainMenuManager manager = canvas.GetComponent<MainMenuManager>();
        if (manager == null)
            manager = canvas.gameObject.AddComponent<MainMenuManager>();

        ClearChildren(canvas.transform);

        Texture2D backgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
        TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MenuFontPath);

        CreateBackground(canvas.transform, backgroundTexture);
        CreateOverlayBars(canvas.transform);
        RectTransform leftShade = CreatePanel("LeftShade", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(560f, 0f), new Color(0f, 0f, 0f, 0.32f));
        CreatePanel("AccentLine", leftShade, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(98f, -150f), new Vector2(110f, 6f), new Color(0.97f, 0.76f, 0.41f, 1f));

        RectTransform contentRoot = CreateRect("ContentRoot", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(78f, 0f), new Vector2(760f, 0f), new Vector2(0f, 0.5f));
        CreateLabel("Kicker", contentRoot, "A GAME ABOUT", menuFont, 42f, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -190f), new Vector2(380f, 56f), TextAlignmentOptions.TopLeft, Color.white);

        TextMeshProUGUI title = CreateLabel("Title", contentRoot, "BUS\nCONDUCTOR", titleFont, 138f, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -260f), new Vector2(820f, 320f), TextAlignmentOptions.TopLeft, Color.white);
        title.lineSpacing = -34f;
        title.characterSpacing = -4f;

        TextMeshProUGUI subtitle = CreateLabel("Subtitle", contentRoot, "Count fares, manage change,\nand keep the route together.", menuFont, 26f, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, -560f), new Vector2(520f, 90f), TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.88f));
        subtitle.textWrappingMode = TextWrappingModes.Normal;
        subtitle.lineSpacing = 8f;

        RectTransform menuRoot = CreateRect("MenuRoot", contentRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 170f), new Vector2(340f, 260f), new Vector2(0f, 0f));
        Button newGameButton = CreateMenuButton("NewGameButton", menuRoot, "NEW GAME", menuFont, new Vector2(0f, 178f));
        Button settingsButton = CreateMenuButton("SettingsButton", menuRoot, "SETTINGS", menuFont, new Vector2(0f, 98f));
        Button quitButton = CreateMenuButton("QuitButton", menuRoot, "QUIT", menuFont, new Vector2(0f, 18f));

        RectTransform footerRoot = CreateRect("FooterRoot", canvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 24f), new Vector2(260f, 40f), new Vector2(1f, 0f));
        CreateLabel("Footer", footerRoot, "Prototype Mockup", menuFont, 22f, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomRight, new Color(1f, 1f, 1f, 0.7f));

        RectTransform settingsOverlay = CreateSettingsOverlay(canvas.transform, titleFont, menuFont);
        Button closeSettingsButton = settingsOverlay.Find("SettingsPanel/BackButton").GetComponent<Button>();

        manager.storyPanel = null;
        manager.settingsPanel = settingsOverlay.gameObject;
        manager.firstSceneName = "GameScene";
        manager.newGameStartingMoney = 100;
        manager.firstSelected = newGameButton;

        ClearPersistentListeners(newGameButton.onClick);
        UnityEventTools.AddPersistentListener(newGameButton.onClick, manager.StartGame);
        ClearPersistentListeners(settingsButton.onClick);
        UnityEventTools.AddPersistentListener(settingsButton.onClick, manager.OpenSettings);
        ClearPersistentListeners(quitButton.onClick);
        UnityEventTools.AddPersistentListener(quitButton.onClick, manager.QuitGame);
        ClearPersistentListeners(closeSettingsButton.onClick);
        UnityEventTools.AddPersistentListener(closeSettingsButton.onClick, manager.CloseSettings);

        EnsureEventSystem(newGameButton);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Build Main Menu Scene")]
    public static void BuildFromMenuItem()
    {
        BuildMainMenuScene();
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);
    }

    private static void EnsureEventSystem(Selectable firstSelected)
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem = eventSystemGo.GetComponent<EventSystem>();
        }

        eventSystem.firstSelectedGameObject = firstSelected != null ? firstSelected.gameObject : null;
    }

    private static void CreateBackground(Transform parent, Texture2D backgroundTexture)
    {
        RectTransform background = CreateRect("Background", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        RawImage rawImage = background.gameObject.AddComponent<RawImage>();
        rawImage.texture = backgroundTexture;
        rawImage.color = Color.white;

        RectTransform wash = CreatePanel("BackgroundWash", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.05f, 0.07f, 0.08f, 0.18f));
        wash.SetAsLastSibling();
    }

    private static void CreateOverlayBars(Transform parent)
    {
        CreatePanel("TopBar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -52f), new Vector2(0f, 104f), new Color(0.08f, 0.08f, 0.08f, 0.92f));
        CreatePanel("BottomBar", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 52f), new Vector2(0f, 104f), new Color(0.11f, 0.08f, 0.06f, 0.88f));
    }

    private static RectTransform CreateSettingsOverlay(Transform parent, TMP_FontAsset titleFont, TMP_FontAsset menuFont)
    {
        RectTransform overlay = CreatePanel("SettingsOverlay", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.68f));

        RectTransform panel = CreatePanel("SettingsPanel", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 340f), new Color(0.08f, 0.10f, 0.11f, 0.97f));
        CreateLabel("SettingsTitle", panel, "SETTINGS", titleFont, 54f, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -26f), new Vector2(-60f, 70f), TextAlignmentOptions.TopLeft, Color.white);
        TextMeshProUGUI body = CreateLabel("SettingsBody", panel, "Static mockup panel.\nWe can wire audio, resolution, and language next.", menuFont, 28f, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -110f), new Vector2(-60f, 120f), TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.85f));
        body.textWrappingMode = TextWrappingModes.Normal;
        body.lineSpacing = 8f;

        CreateMenuButton("BackButton", panel, "BACK", menuFont, new Vector2(30f, 28f), new Vector2(220f, 58f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        overlay.gameObject.SetActive(false);
        return overlay;
    }

    private static Button CreateMenuButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 anchoredPosition)
    {
        return CreateMenuButton(name, parent, label, font, anchoredPosition, new Vector2(320f, 60f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
    }

    private static Button CreateMenuButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform buttonRect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, size, new Vector2(0f, 0.5f));
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.02f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.02f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.10f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.04f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.targetGraphic = buttonImage;

        CreatePanel("Accent", buttonRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(6f, 38f), new Color(0.97f, 0.76f, 0.41f, 1f));
        TextMeshProUGUI labelText = CreateLabel("Label", buttonRect, label, font, 34f, FontStyles.Bold, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 0f), new Vector2(-28f, 0f), TextAlignmentOptions.MidlineLeft, Color.white);
        labelText.characterSpacing = 6f;

        return button;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta, new Vector2(
            Mathf.Approximately(anchorMin.x, anchorMax.x) ? anchorMin.x : 0.5f,
            Mathf.Approximately(anchorMin.y, anchorMax.y) ? anchorMin.y : 0.5f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, string textValue, TMP_FontAsset font, float size, FontStyles style, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta, new Vector2(
            Mathf.Approximately(anchorMin.x, anchorMax.x) ? anchorMin.x : 0.5f,
            Mathf.Approximately(anchorMin.y, anchorMax.y) ? anchorMin.y : 0.5f));
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static void ClearPersistentListeners(UnityEngine.Events.UnityEvent unityEvent)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(unityEvent, i);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.pivot = pivot;
        rect.localScale = Vector3.one;
        return rect;
    }
}
