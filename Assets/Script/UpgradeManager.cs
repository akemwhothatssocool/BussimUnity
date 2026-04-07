using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("=== หน้าต่าง UI ===")]
    public GameObject upgradePanel;
    public GameObject phoneShopPanel;
    public TextMeshProUGUI txtTotalMoney;
    public Button btnTogglePhone;
    public TextMeshProUGUI txtToggleHint;
    public string toggleHintMessage = "";
    public string toggleHotkeyLabel = "B";

    [Header("=== 1. อัปเกรดเครื่องยนต์ (Engine - เพิ่มความเร็ว) ===")]
    public int engineUpgradeCost = 160;
    public float engineSpeedIncreaseAmount = 2f;
    public TextMeshProUGUI txtEngineCost;

    [Header("=== 2. Fuel Upgrade (Increase Stops Per Day) ===")]
    public int fuelUpgradeCost = 140;
    public int stopIncreasePerFuelUpgrade = 1;
    public int maxStopsPerDay = 14;
    public TextMeshProUGUI txtFuelCost;

    [Header("=== 3. อัปเกรดเบาะนั่ง (Seat - เพิ่มความนิยม) ===")]
    public int seatUpgradeCost = 220;
    public float popularityBoost = 5f;
    public TextMeshProUGUI txtSeatCost;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        ResolvePanels();
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (phoneShopPanel != null) phoneShopPanel.SetActive(false);
    }

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (phoneShopPanel != null) phoneShopPanel.SetActive(false);
        EnsureToggleHudButton();
        RefreshToggleHudButton();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && CanTogglePhoneMenu())
            TogglePhoneMenu();

        RefreshToggleHudButton();
    }

    public void OpenMenu()
    {
        OpenUpgradePanel();
    }

    public void OpenUpgradePanel()
    {
        if (!CanOpenUpgradePanel())
            return;

        SetUpgradePanelVisible(true);
        UpdateUI();
    }

    public void CloseMenu()
    {
        CloseAllMenus();
    }

    public void ToggleMenu()
    {
        TogglePhoneMenu();
    }

    public void TogglePhoneMenu()
    {
        SetPhoneShopVisible(!IsPhoneShopOpen());
        if (IsPhoneShopOpen())
            UpdateUI();
    }

    public void UpdateUI()
    {
        RefreshMoneyLabels();

        if (txtEngineCost) txtEngineCost.text = $"{engineUpgradeCost}";
        if (txtFuelCost) txtFuelCost.text = CanBuyFuelUpgrade() ? $"{fuelUpgradeCost}" : "MAX";
        if (txtSeatCost) txtSeatCost.text = $"{seatUpgradeCost}";
    }

    public void ApplySaveData(GameSaveData data)
    {
        if (data == null) return;

        engineUpgradeCost = data.engineUpgradeCost;
        fuelUpgradeCost = data.fuelUpgradeCost;
        seatUpgradeCost = data.seatUpgradeCost;
        UpdateUI();
    }

    bool CanBuyFuelUpgrade()
    {
        return GameManager.Instance == null || GameManager.Instance.stopsPerDay < maxStopsPerDay;
    }

    public void BuyEngineUpgrade()
    {
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.currentMoney >= engineUpgradeCost)
        {
            if (!PlayerWallet.Instance.SpendMoney(engineUpgradeCost))
                return;

            float totalEngineBonus = 0f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.engineSpeedBonus += engineSpeedIncreaseAmount;
                totalEngineBonus = GameManager.Instance.engineSpeedBonus;
            }

            CityManager cityManager = Object.FindFirstObjectByType<CityManager>();
            if (cityManager != null)
                cityManager.ApplyEngineSpeedBonus(totalEngineBonus);

            Debug.Log($"อัปเกรดเครื่องยนต์สำเร็จ! ความเร็วโบนัสรวม +{totalEngineBonus}");

            engineUpgradeCost += 200;
            UpdateUI();
            SaveSystem.SaveCurrentGame();
        }
    }

    public void BuyFuelUpgrade()
    {
        if (!CanBuyFuelUpgrade())
            return;

        if (PlayerWallet.Instance != null && PlayerWallet.Instance.currentMoney >= fuelUpgradeCost)
        {
            if (!PlayerWallet.Instance.SpendMoney(fuelUpgradeCost))
                return;

            if (GameManager.Instance != null)
                GameManager.Instance.stopsPerDay = Mathf.Min(maxStopsPerDay, GameManager.Instance.stopsPerDay + stopIncreasePerFuelUpgrade);

            fuelUpgradeCost += 180;
            UpdateUI();
            Debug.Log($"Fuel upgrade purchased! Daily route limit is now {GameManager.Instance.stopsPerDay} stops.");
            SaveSystem.SaveCurrentGame();
        }
    }

    public void BuySeatUpgrade()
    {
        if (PlayerWallet.Instance != null && PlayerWallet.Instance.currentMoney >= seatUpgradeCost)
        {
            if (!PlayerWallet.Instance.SpendMoney(seatUpgradeCost))
                return;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.permanentPopularityBonus += popularityBoost;

                if (GameManager.Instance.busRateDisplay != null)
                {
                    GameManager.Instance.busRateDisplay.UpdateBusRate(GameManager.Instance.GetStarRating());
                }
            }

            seatUpgradeCost += 200;
            UpdateUI();
            Debug.Log($"อัปเกรดเบาะสำเร็จ! ได้โบนัสถาวร +{popularityBoost}%");
            SaveSystem.SaveCurrentGame();
        }
    }

    void ResolvePanels()
    {
        if (upgradePanel == null)
            upgradePanel = FindPanelByName("UpgradePanel");

        if (phoneShopPanel == null)
            phoneShopPanel = FindPanelByName("PhoneShop_Panel");

        if (txtToggleHint == null)
            txtToggleHint = FindObjectByName<TextMeshProUGUI>("PhoneToggleHint");

        if (btnTogglePhone == null)
            btnTogglePhone = FindObjectByName<Button>("PhoneToggleButton");

        if (txtTotalMoney == null)
            txtTotalMoney = FindMoneyLabel(upgradePanel) ?? FindMoneyLabel(phoneShopPanel);
    }

    T FindObjectByName<T>(string objectName) where T : Component
    {
        T[] objects = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (T obj in objects)
        {
            if (obj != null && obj.name == objectName)
                return obj;
        }

        return null;
    }

    GameObject FindPanelByName(string panelName)
    {
        Transform panel = FindObjectByName<Transform>(panelName);
        return panel != null ? panel.gameObject : null;
    }

    TextMeshProUGUI FindMoneyLabel(GameObject panel)
    {
        if (panel == null)
            return null;

        TextMeshProUGUI[] labels = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label != null && label.name == "Txt_TotalMoney")
                return label;
        }

        return null;
    }

    void RefreshMoneyLabels()
    {
        if (PlayerWallet.Instance == null)
            return;

        string moneyText = PlayerWallet.Instance.currentMoney.ToString("N0");

        if (txtTotalMoney != null)
            txtTotalMoney.text = moneyText;

        HashSet<TextMeshProUGUI> updatedLabels = new HashSet<TextMeshProUGUI>();
        if (txtTotalMoney != null)
            updatedLabels.Add(txtTotalMoney);

        UpdateMoneyLabelsInPanel(upgradePanel, moneyText, updatedLabels);
        UpdateMoneyLabelsInPanel(phoneShopPanel, moneyText, updatedLabels);
    }

    void UpdateMoneyLabelsInPanel(GameObject panel, string moneyText, HashSet<TextMeshProUGUI> updatedLabels)
    {
        if (panel == null)
            return;

        TextMeshProUGUI[] labels = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label == null || label.name != "Txt_TotalMoney" || updatedLabels.Contains(label))
                continue;

            label.text = moneyText;
            updatedLabels.Add(label);
        }
    }

    bool IsMenuOpen()
    {
        return IsUpgradePanelOpen() || IsPhoneShopOpen();
    }

    bool IsUpgradePanelOpen()
    {
        return upgradePanel != null && upgradePanel.activeSelf;
    }

    bool IsPhoneShopOpen()
    {
        return phoneShopPanel != null && phoneShopPanel.activeSelf;
    }

    bool IsSummaryOpen()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.summaryPanel != null &&
               GameManager.Instance.summaryPanel.activeInHierarchy;
    }

    bool CanOpenUpgradePanel()
    {
        if (IsUpgradePanelOpen())
            return true;

        return IsSummaryOpen();
    }

    bool CanTogglePhoneMenu()
    {
        if (IsPhoneShopOpen())
            return true;

        if (!IsSummaryOpen() && Time.timeScale <= 0f)
            return false;

        FareSystem fareSystem = Object.FindFirstObjectByType<FareSystem>();
        if (fareSystem != null && fareSystem.IsBusy)
            return false;

        return true;
    }

    void SetUpgradePanelVisible(bool visible, bool updateCursor = true)
    {
        if (upgradePanel != null) upgradePanel.SetActive(visible);

        if (!updateCursor)
            return;

        UpdateCursorState();
    }

    void SetPhoneShopVisible(bool visible, bool updateCursor = true)
    {
        if (phoneShopPanel != null) phoneShopPanel.SetActive(visible);

        if (!updateCursor)
            return;

        UpdateCursorState();
    }

    void CloseAllMenus()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (phoneShopPanel != null) phoneShopPanel.SetActive(false);
        UpdateCursorState();
    }

    void UpdateCursorState()
    {
        if (IsMenuOpen())
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        bool keepCursorVisible = IsSummaryOpen();

        Cursor.visible = keepCursorVisible;
        Cursor.lockState = keepCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    void EnsureToggleHudButton()
    {
        if (btnTogglePhone != null)
            return;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject buttonObject = new GameObject("PhoneToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.layer = canvas.gameObject.layer;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(canvas.transform, false);
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-28f, 24f);
        buttonRect.sizeDelta = new Vector2(184f, 76f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.09f, 0.11f, 0.15f, 0.92f);

        btnTogglePhone = buttonObject.GetComponent<Button>();
        btnTogglePhone.targetGraphic = buttonImage;
        btnTogglePhone.onClick.AddListener(HandleHudButtonPressed);

        ColorBlock colors = btnTogglePhone.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.98f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.98f, 1f);
        colors.selectedColor = new Color(0.96f, 0.98f, 1f, 1f);
        colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.6f);
        btnTogglePhone.colors = colors;

        CreateHudImage("Glow", buttonRect, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.22f, 0.79f, 0.74f, 0.16f));
        CreateHudImage("SideStripe", buttonRect, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f), new Color(0.22f, 0.79f, 0.74f, 0.98f));
        CreateHudImage("PhoneBody", buttonRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(34f, 48f), new Color(0.17f, 0.20f, 0.26f, 1f));
        CreateHudImage("PhoneScreen", buttonRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(24f, 34f), new Color(0.55f, 0.95f, 0.88f, 1f));

        txtToggleHint = CreateHudLabel("PhoneToggleHint", buttonRect, toggleHintMessage, 26f, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(62f, -6f), new Vector2(-54f, -12f), TextAlignmentOptions.MidlineLeft, Color.white);
        txtToggleHint.fontStyle = FontStyles.Bold;

        TextMeshProUGUI hotkeyLabel = CreateHudLabel("PhoneToggleHotkey", buttonRect, toggleHotkeyLabel, 24f, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(34f, 34f), TextAlignmentOptions.Center, new Color(0.07f, 0.09f, 0.12f, 1f));
        hotkeyLabel.fontStyle = FontStyles.Bold;
        CreateHudImage("HotkeyBadge", hotkeyLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.96f, 0.84f, 0.46f, 1f)).SetAsFirstSibling();
    }

    void RefreshToggleHudButton()
    {
        if (btnTogglePhone == null)
            return;

        bool phoneOpen = IsPhoneShopOpen();
        bool visible = phoneOpen || CanTogglePhoneMenu();

        btnTogglePhone.gameObject.SetActive(visible);
        btnTogglePhone.interactable = visible;

        Image buttonImage = btnTogglePhone.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = phoneOpen
                ? new Color(0.12f, 0.18f, 0.22f, 0.96f)
                : new Color(0.09f, 0.11f, 0.15f, 0.92f);
        }

        if (txtToggleHint != null)
        {
            txtToggleHint.text = phoneOpen ? string.Empty : toggleHintMessage;
            txtToggleHint.gameObject.SetActive(!string.IsNullOrWhiteSpace(txtToggleHint.text));
        }
    }

    void HandleHudButtonPressed()
    {
        if (CanTogglePhoneMenu())
            TogglePhoneMenu();
    }

    RectTransform CreateHudImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    TextMeshProUGUI CreateHudLabel(string name, Transform parent, string textValue, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }
}
