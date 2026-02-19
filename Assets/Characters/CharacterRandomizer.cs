using UnityEngine;
using System.Collections; // ✅ จำเป็นสำหรับ Coroutine

public class CharacterRandomizer : MonoBehaviour
{
    [Header("---- ส่วนที่ 1: โมเดล (Mesh) ----")]
    public GameObject[] hairStyles;
    public GameObject[] noses;
    public GameObject[] eyes;
    public GameObject[] shirts;
    public GameObject[] pants;
    public GameObject[] shoes;

    [Header("---- ส่วนที่ 2: สี (Colors) ----")]
    [Range(0f, 1f)] public float roughness = 1f;
    public bool randomizeOnStart = true;

    [Header("👃 สีจมูก & ตา")]
    [Range(0f, 1f)] public float noseDarkerAmount = 0.2f;
    public Color defaultEyeColor = Color.black;

    // --- ✅ ส่วนที่เพิ่มใหม่: ระบบตากระพริบ ---
    [Header("👀 ตั้งค่าการกระพริบตา")]
    public bool enableBlinking = true;
    public float blinkIntervalMin = 2f; // เว้นช่วงสุ่มตั้งแต่ 2 วิ
    public float blinkIntervalMax = 5f; // ถึง 5 วิ
    public float blinkDuration = 0.15f; // ความเร็วในการหลับตา (ยิ่งน้อยยิ่งเร็ว)
    [Range(0f, 1f)] public float closedScaleY = 0.1f; // ขนาดแกน Y ตอนหลับตา (0.1 คือเกือบปิดสนิท)

    private float blinkTimer;
    private Vector3 originalEyeScale; // จำขนาดตาเดิมไว้
    private bool isBlinking = false;
    // --------------------------------------

    [Header("🤪 โหมดฮาๆ สุดๆ")]
    public bool useCrazyMode = true;
    public bool autoLoopCrazy = false;
    public float loopInterval = 0.3f;
    [Range(0f, 1f)] public float crazyLevel = 0.7f;

    private Renderer currentHairRenderer;
    private Renderer currentNoseRenderer;
    private Renderer currentEyeRenderer;
    private Renderer currentShirtRenderer;
    private Renderer currentPantsRenderer;
    private Renderer currentShoesRenderer;

    [Header("ลาก Body (ผิวหนัง) ใส่ตรงนี้")]
    [SerializeField] private Renderer skinRenderer;

    void Awake()
    {
        if (randomizeOnStart)
            RandomizeCharacter();

        if (autoLoopCrazy)
            StartCrazyLoop();
    }

    // --- ✅ เพิ่ม Update เพื่อคอยจับเวลากระพริบตา ---
    void Update()
    {
        if (!enableBlinking || currentEyeRenderer == null || isBlinking) return;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            StartCoroutine(BlinkRoutine());
            ResetBlinkTimer();
        }
    }
    // -------------------------------------------

    public void RandomizeCharacter()
    {
        // 1. สุ่มเปิดโมเดล
        currentHairRenderer = RandomizePart(hairStyles);
        currentNoseRenderer = RandomizePart(noses);
        currentEyeRenderer = RandomizePart(eyes);
        currentShirtRenderer = RandomizePart(shirts);
        currentPantsRenderer = RandomizePart(pants);
        currentShoesRenderer = RandomizePart(shoes);

        // ✅ เมื่อได้ตาใหม่มา ต้องจำขนาดเดิม และรีเซ็ตเวลากระพริบ
        if (currentEyeRenderer != null)
        {
            originalEyeScale = currentEyeRenderer.transform.localScale;
            ResetBlinkTimer();
            isBlinking = false; // กันเหนียวเผื่อเปลี่ยนตอนกำลังหลับตา
        }

        // 2. สุ่มสี
        if (useCrazyMode)
            RandomizeCrazy();
        else
            RandomizeNormal();

        // 3. ปรับความด้าน
        ApplyRoughness();
    }

    // --- ✅ ฟังก์ชันสำหรับกระพริบตา ---
    IEnumerator BlinkRoutine()
    {
        isBlinking = true;
        Transform eyeTr = currentEyeRenderer.transform;

        // 1. หลับตา (ย่อแกน Y)
        Vector3 closedState = originalEyeScale;
        closedState.y = closedScaleY; // ย่อเหลือตามที่ตั้งไว้
        eyeTr.localScale = closedState;

        // 2. รอแป๊บนึง
        yield return new WaitForSeconds(blinkDuration);

        // 3. ลืมตา (คืนค่าเดิม)
        // เช็คอีกรอบเผื่อตานั้นถูกทำลายไประหว่างรอ
        if (eyeTr != null)
        {
            eyeTr.localScale = originalEyeScale;
        }

        isBlinking = false;
    }

    void ResetBlinkTimer()
    {
        blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
    }
    // -------------------------------------------


    Renderer RandomizePart(GameObject[] parts)
    {
        if (parts == null || parts.Length == 0) return null;

        foreach (var p in parts)
        {
            if (p != null) p.SetActive(false);
        }

        int randIndex = Random.Range(0, parts.Length);
        if (parts[randIndex] != null)
        {
            parts[randIndex].SetActive(true);
            return parts[randIndex].GetComponent<Renderer>();
        }
        return null;
    }

    void RandomizeCrazy()
    {
        float roll = Random.value;

        // 🎨 15% → สีเดียวทั้งตัว
        if (roll < 0.15f * crazyLevel)
        {
            Color oneColor = RandomRGB();
            SetAllColors(oneColor);
            ApplyNoseDarkness(oneColor);

            if (currentEyeRenderer != null)
                currentEyeRenderer.material.color = new Color(1 - oneColor.r, 1 - oneColor.g, 1 - oneColor.b);

            return;
        }

        // 🌈 15% → สีรุ้ง
        if (roll < 0.30f * crazyLevel)
        {
            if (currentHairRenderer != null) currentHairRenderer.material.color = Color.red;
            if (currentShirtRenderer != null) currentShirtRenderer.material.color = Color.yellow;
            if (currentPantsRenderer != null) currentPantsRenderer.material.color = Color.green;
            if (currentShoesRenderer != null) currentShoesRenderer.material.color = Color.blue;

            Color skinColor = Random.value < 0.5f ? Color.magenta : new Color(1f, 0.5f, 0f);
            if (skinRenderer != null) skinRenderer.material.color = skinColor;
            ApplyNoseDarkness(skinColor);

            if (currentEyeRenderer != null) currentEyeRenderer.material.color = Color.white;
            return;
        }

        // 🔄 10% → สีตรงข้าม
        if (roll < 0.40f * crazyLevel)
        {
            Color top = RandomRGB();
            Color bottom = new Color(1f - top.r, 1f - top.g, 1f - top.b);

            if (currentHairRenderer != null) currentHairRenderer.material.color = top;
            if (currentShirtRenderer != null) currentShirtRenderer.material.color = top;
            if (currentPantsRenderer != null) currentPantsRenderer.material.color = bottom;
            if (currentShoesRenderer != null) currentShoesRenderer.material.color = bottom;

            Color skinColor = Random.value < 0.3f ? RandomRGB() : Color.HSVToRGB(0.08f, 0.3f, Random.Range(0.2f, 0.95f));
            if (skinRenderer != null) skinRenderer.material.color = skinColor;
            ApplyNoseDarkness(skinColor);

            if (currentEyeRenderer != null) currentEyeRenderer.material.color = defaultEyeColor;
            return;
        }

        // 💥 10% → นีออนสุดฉูดฉาด
        if (roll < 0.50f * crazyLevel)
        {
            if (currentHairRenderer != null) currentHairRenderer.material.color = RandomNeon();
            if (currentShirtRenderer != null) currentShirtRenderer.material.color = RandomNeon();
            if (currentPantsRenderer != null) currentPantsRenderer.material.color = RandomNeon();
            if (currentShoesRenderer != null) currentShoesRenderer.material.color = RandomNeon();

            Color skinColor = Random.value < 0.5f ? RandomNeon() : Color.HSVToRGB(0.08f, 0.3f, Random.Range(0.2f, 0.95f));
            if (skinRenderer != null) skinRenderer.material.color = skinColor;
            ApplyNoseDarkness(skinColor);

            if (currentEyeRenderer != null) currentEyeRenderer.material.color = Color.white;
            return;
        }

        // ⚡ โหมดปกติแบบฮา (50%)
        Color hairColor = RandomRGB();
        if (currentHairRenderer != null)
            currentHairRenderer.material.color = hairColor;

        if (currentShirtRenderer != null)
            currentShirtRenderer.material.color = Random.value < 0.3f ? hairColor : RandomRGB();

        if (currentPantsRenderer != null)
            currentPantsRenderer.material.color = RandomRGB();

        if (currentShoesRenderer != null)
            currentShoesRenderer.material.color = RandomRGB();

        Color skinColor2;
        if (Random.value < 0.4f * crazyLevel)
            skinColor2 = RandomRGB();
        else
            skinColor2 = Color.HSVToRGB(0.08f, 0.3f, Random.Range(0.2f, 0.95f));

        if (skinRenderer != null) skinRenderer.material.color = skinColor2;
        ApplyNoseDarkness(skinColor2);

        if (currentEyeRenderer != null) currentEyeRenderer.material.color = defaultEyeColor;
    }

    void RandomizeNormal()
    {
        float brightness = Random.Range(0.2f, 0.95f);
        Color skinColor = Color.HSVToRGB(0.08f, 0.3f, brightness);

        if (skinRenderer != null) skinRenderer.material.color = skinColor;
        ApplyNoseDarkness(skinColor);

        if (currentEyeRenderer != null) currentEyeRenderer.material.color = defaultEyeColor;

        if (currentHairRenderer != null)
        {
            Color[] normalHair = {
                Color.black,
                new Color(0.3f, 0.2f, 0.1f),
                new Color(0.9f, 0.8f, 0.6f),
                new Color(0.6f, 0.3f, 0.1f),
                Color.gray
            };
            currentHairRenderer.material.color = normalHair[Random.Range(0, normalHair.Length)];
        }

        if (currentShirtRenderer != null)
            currentShirtRenderer.material.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 1f);

        if (currentPantsRenderer != null)
            currentPantsRenderer.material.color = Random.ColorHSV(0f, 1f, 0.2f, 0.6f, 0.3f, 0.7f);

        if (currentShoesRenderer != null)
        {
            if (Random.value < 0.4f)
                currentShoesRenderer.material.color = Random.value > 0.5f ? Color.white : Color.black;
            else
                currentShoesRenderer.material.color = Random.ColorHSV(0f, 1f, 0.4f, 1f, 0.5f, 1f);
        }
    }

    void ApplyNoseDarkness(Color baseSkinColor)
    {
        if (currentNoseRenderer == null) return;
        Color.RGBToHSV(baseSkinColor, out float h, out float s, out float v);
        v = Mathf.Max(0f, v - noseDarkerAmount);
        currentNoseRenderer.material.color = Color.HSVToRGB(h, s, v);
    }

    Color RandomRGB() => new Color(Random.value, Random.value, Random.value, 1f);
    Color RandomNeon() => Color.HSVToRGB(Random.value, 1f, 1f);

    void SetAllColors(Color c)
    {
        if (currentHairRenderer != null) currentHairRenderer.material.color = c;
        if (currentShirtRenderer != null) currentShirtRenderer.material.color = c;
        if (currentPantsRenderer != null) currentPantsRenderer.material.color = c;
        if (currentShoesRenderer != null) currentShoesRenderer.material.color = c;
        if (skinRenderer != null) skinRenderer.material.color = c;
    }

    void ApplyRoughness()
    {
        float smoothness = 1f - roughness;
        SetSmoothness(currentHairRenderer, smoothness);
        SetSmoothness(currentNoseRenderer, smoothness);
        SetSmoothness(currentEyeRenderer, smoothness);
        SetSmoothness(currentShirtRenderer, smoothness);
        SetSmoothness(currentPantsRenderer, smoothness);
        SetSmoothness(currentShoesRenderer, smoothness);
        SetSmoothness(skinRenderer, smoothness);
    }

    void SetSmoothness(Renderer r, float val)
    {
        if (r == null) return;
        Material mat = r.material;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", val);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", val);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
    }

    public void StartCrazyLoop()
    {
        CancelInvoke(nameof(RandomizeCharacter));
        useCrazyMode = true;
        InvokeRepeating(nameof(RandomizeCharacter), 0f, loopInterval);
    }

    public void StopCrazyLoop()
    {
        CancelInvoke(nameof(RandomizeCharacter));
    }
}