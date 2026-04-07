using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpgradeShop : MonoBehaviour
{
    enum ShopCategory
    {
        Chair = 0,
        Fan = 1
    }

    const string SeatTitleObjectNameLv1 = "Chair_Lv.1";
    const string SeatTitleObjectNameLv2 = "Chair_Lv.2";
    const string SeatTitleObjectNameLv3 = "Chair_Lv.3";
    const string SeatPriceObjectNameLv1 = "Chair_Lv1_price";
    const string SeatPriceObjectNameLv2 = "Chair_Lv2_price";
    const string SeatPriceObjectNameLv3 = "Chair_Lv3_price";
    const string CategoryDropdownObjectName = "Dropdown";
    const string DefaultChairFeedback = "เลือกเก้าอี้ที่ต้องการซื้อ";
    const string DefaultFanFeedback = "<color=#7A6B57>Spray กำลังเตรียมใช้งาน</color>";
    const string DeliveryUnavailableFeedback = "<color=red>ระบบส่งเก้าอี้ยังไม่พร้อมใช้งาน</color>";

    static readonly string[] ChairCardObjectNames =
    {
        "CardBaseLV1",
        "CardBaseLV2",
        "CardBaseLV3"
    };

    static readonly string[] FanCardObjectNames =
    {
        "SprayCardBaseLV1",
        "FanCardBaseLV1",
        "FanCardBaseLV2",
        "FanCardBaseLV3",
        "FanCardLV1",
        "FanCardLV2",
        "FanCardLV3",
        "CardBaseFanLV1",
        "CardBaseFanLV2",
        "CardBaseFanLV3"
    };

    [Header("Seat Prices")]
    public int priceLv1 = 80;
    public int priceLv2 = 180;
    public int priceLv3 = 350;

    [Header("Object Prices")]
    public int sprayPrice = 60;

    [Header("Feedback UI")]
    public TextMeshProUGUI txtShopFeedback;

    TMP_Dropdown categoryDropdown;
    GameObject[] chairCards;
    GameObject[] fanCards;

    void Awake()
    {
        CacheUiReferences();
        RefreshShopUi();
        ApplySelectedCategory();
    }

    void OnEnable()
    {
        CacheUiReferences();
        RefreshShopUi();
        ApplySelectedCategory();
    }

    void OnDestroy()
    {
        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.RemoveListener(HandleCategoryChanged);
    }

    public void BuySeat(int level)
    {
        if (GetSelectedCategory() != ShopCategory.Chair)
        {
            SprayDeliveryManager sprayDeliveryManager = SprayDeliveryManager.GetOrCreateInstance();
            if (sprayDeliveryManager == null)
            {
                SetFeedback(DefaultFanFeedback);
                return;
            }

            if (sprayDeliveryManager.TryOrderSprayDelivery(sprayPrice, out string sprayFeedback))
            {
                SetFeedback(sprayFeedback);

                if (UpgradeManager.Instance != null)
                    UpgradeManager.Instance.CloseMenu();

                return;
            }

            SetFeedback(sprayFeedback);
            return;
        }

        SeatDeliveryManager deliveryManager = SeatDeliveryManager.GetOrCreateInstance();
        if (deliveryManager == null)
        {
            SetFeedback(DeliveryUnavailableFeedback);
            return;
        }

        int cost = GetPriceForLevel(level);
        if (deliveryManager.TryOrderSeatDelivery(level, cost, out string feedback))
        {
            SetFeedback(feedback);

            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.CloseMenu();

            return;
        }

        SetFeedback(feedback);
    }

    public void BuyNewSeat()
    {
        BuySeat(1);
    }

    int GetPriceForLevel(int level)
    {
        level = Mathf.Clamp(level, 1, BusSeat.MaxSupportedSeatLevel);
        return level switch
        {
            1 => priceLv1,
            2 => priceLv2,
            3 => priceLv3,
            _ => 0
        };
    }

    void SetFeedback(string message)
    {
        if (txtShopFeedback != null)
            txtShopFeedback.text = message;
    }

    void RefreshShopUi()
    {
        SetupCategoryDropdown();

        SetTextByObjectName(SeatTitleObjectNameLv1, "Chair Lv.1");
        SetTextByObjectName(SeatTitleObjectNameLv2, "Chair Lv.2");
        SetTextByObjectName(SeatTitleObjectNameLv3, "Chair Lv.3");

        SetTextByObjectName(SeatPriceObjectNameLv1, priceLv1.ToString());
        SetTextByObjectName(SeatPriceObjectNameLv2, priceLv2.ToString());
        SetTextByObjectName(SeatPriceObjectNameLv3, priceLv3.ToString());
        SetTextByObjectName("Spray_price", sprayPrice.ToString());
    }

    void CacheUiReferences()
    {
        categoryDropdown ??= FindDropdownByObjectName(CategoryDropdownObjectName);
        chairCards ??= FindObjectsByNames(ChairCardObjectNames);
        fanCards ??= FindObjectsByNames(FanCardObjectNames, "fan", "card");
    }

    void SetupCategoryDropdown()
    {
        if (categoryDropdown == null)
            return;

        int selectedIndex = Mathf.Clamp(categoryDropdown.value, 0, 1);
        categoryDropdown.onValueChanged.RemoveListener(HandleCategoryChanged);

        if (categoryDropdown.options.Count != 2 ||
            categoryDropdown.options[0].text != "Chair" ||
            categoryDropdown.options[1].text != "Object")
        {
            categoryDropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new("Chair"),
                new("Object")
            };
        }

        categoryDropdown.SetValueWithoutNotify(selectedIndex);
        categoryDropdown.RefreshShownValue();
        categoryDropdown.onValueChanged.AddListener(HandleCategoryChanged);
    }

    void HandleCategoryChanged(int selectedIndex)
    {
        ApplyCategory((ShopCategory)Mathf.Clamp(selectedIndex, 0, 1));
    }

    void ApplySelectedCategory()
    {
        ApplyCategory(GetSelectedCategory());
    }

    ShopCategory GetSelectedCategory()
    {
        if (categoryDropdown == null)
            return ShopCategory.Chair;

        return (ShopCategory)Mathf.Clamp(categoryDropdown.value, 0, 1);
    }

    void ApplyCategory(ShopCategory category)
    {
        bool showChairCards = category == ShopCategory.Chair;
        bool showFanCards = category == ShopCategory.Fan && fanCards.Length > 0;

        SetActiveState(chairCards, showChairCards);
        SetActiveState(fanCards, showFanCards);

        if (category == ShopCategory.Fan && !showFanCards)
        {
            SetFeedback(DefaultFanFeedback);
            return;
        }

        if (txtShopFeedback == null)
            return;

        if (string.IsNullOrWhiteSpace(txtShopFeedback.text) ||
            txtShopFeedback.text == DefaultChairFeedback ||
            txtShopFeedback.text == DefaultFanFeedback)
        {
            SetFeedback(showChairCards ? DefaultChairFeedback : string.Empty);
        }
    }

    void SetTextByObjectName(string objectName, string value)
    {
        TextMeshProUGUI targetText = FindTextByObjectName(objectName);
        if (targetText != null)
            targetText.text = value;
    }

    TMP_Dropdown FindDropdownByObjectName(string objectName)
    {
        TMP_Dropdown[] allDropdowns = Resources.FindObjectsOfTypeAll<TMP_Dropdown>();
        foreach (TMP_Dropdown dropdown in allDropdowns)
        {
            if (!dropdown.gameObject.scene.IsValid())
                continue;

            if (dropdown.gameObject.name == objectName)
                return dropdown;
        }

        return null;
    }

    TextMeshProUGUI FindTextByObjectName(string objectName)
    {
        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in allTexts)
        {
            if (!text.gameObject.scene.IsValid())
                continue;

            if (text.gameObject.name == objectName)
                return text;
        }

        return null;
    }

    GameObject[] FindObjectsByNames(string[] objectNames, params string[] fallbackKeywords)
    {
        List<GameObject> results = new();
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (string objectName in objectNames)
        {
            foreach (Transform transform in allTransforms)
            {
                if (!transform.gameObject.scene.IsValid())
                    continue;

                if (transform.gameObject.name != objectName)
                    continue;

                if (!results.Contains(transform.gameObject))
                    results.Add(transform.gameObject);
            }
        }

        if (results.Count == 0 && fallbackKeywords != null && fallbackKeywords.Length > 0)
        {
            foreach (Transform transform in allTransforms)
            {
                if (!transform.gameObject.scene.IsValid())
                    continue;

                string objectName = transform.gameObject.name.ToLowerInvariant();
                bool matchesAllKeywords = true;

                foreach (string keyword in fallbackKeywords)
                {
                    if (!objectName.Contains(keyword))
                    {
                        matchesAllKeywords = false;
                        break;
                    }
                }

                if (matchesAllKeywords && !results.Contains(transform.gameObject))
                    results.Add(transform.gameObject);
            }
        }

        return results.ToArray();
    }

    void SetActiveState(GameObject[] objects, bool active)
    {
        foreach (GameObject target in objects)
        {
            if (target == null)
                continue;

            if (target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
