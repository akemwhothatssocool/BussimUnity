using UnityEngine;

public class SprayDeliveryItem : MonoBehaviour, IInteractable
{
    const string BoxDropClipResourcePath = "Sound/BoxDrop";

    static AudioClip cachedBoxDropClip;
    static Material cachedSprayEffectMaterial;
    static Texture2D cachedSprayEffectTexture;
    [SerializeField] float boxDropVolume = 0.45f;
    [SerializeField] float minDropImpactSpeed = 0.55f;

    Rigidbody itemBody;
    Collider itemCollider;
    Transform visualRoot;
    ParticleSystem sprayEffect;
    bool playDropSoundOnNextCollision;
    bool hasPlayedDropSound;
    int remainingUses;

    void Awake()
    {
        itemBody = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
    }

    public void Setup(GameObject visualPrefab, Vector3 visualLocalScale, Vector3 visualLocalEuler, int initialUses)
    {
        name = "SprayDeliveryItem";
        remainingUses = Mathf.Max(1, initialUses);
        EnsureVisualRoot();
        EnsureSprayEffect();

        if (visualPrefab != null)
        {
            GameObject visualInstance = Instantiate(visualPrefab, visualRoot);
            visualInstance.name = "SprayVisual";
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.Euler(visualLocalEuler);
            visualInstance.transform.localScale = visualLocalScale;
            SetLayerRecursively(visualInstance, gameObject.layer);
            return;
        }

        GameObject fallbackVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fallbackVisual.name = "SprayVisualFallback";
        fallbackVisual.transform.SetParent(visualRoot, false);
        fallbackVisual.transform.localPosition = Vector3.zero;
        fallbackVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        fallbackVisual.transform.localScale = new Vector3(0.12f, 0.26f, 0.12f);
        Collider fallbackCollider = fallbackVisual.GetComponent<Collider>();
        if (fallbackCollider != null)
            Destroy(fallbackCollider);

        Renderer fallbackRenderer = fallbackVisual.GetComponent<Renderer>();
        if (fallbackRenderer != null)
            fallbackRenderer.material.color = new Color(0.42f, 0.86f, 0.72f, 1f);
    }

    public void SetCarryVisualPose(Vector3 localPosition, Vector3 localEuler)
    {
        EnsureVisualRoot();
        if (visualRoot == null)
            return;

        visualRoot.localPosition = localPosition;
        visualRoot.localRotation = Quaternion.Euler(localEuler);
    }

    public void ResetVisualPose()
    {
        EnsureVisualRoot();
        if (visualRoot == null)
            return;

        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
    }

    public void SetSprayEffectActive(bool active)
    {
        EnsureSprayEffect();
        if (sprayEffect == null)
            return;

        if (active)
        {
            if (!sprayEffect.isPlaying)
                sprayEffect.Play(true);
            return;
        }

        if (sprayEffect.isPlaying)
            sprayEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public int GetRemainingUses()
    {
        return remainingUses;
    }

    public int ConsumeUse()
    {
        remainingUses = Mathf.Max(0, remainingUses - 1);
        return remainingUses;
    }

    public void ArmDropSound()
    {
        playDropSoundOnNextCollision = true;
        hasPlayedDropSound = false;
    }

    public bool CanInteract()
    {
        if (transform.parent != null)
            return false;

        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        return player != null && !player.IsCarryingSeatPackage() && !player.IsCarryingSprayItem();
    }

    public void Interact()
    {
        SprayDeliveryManager manager = SprayDeliveryManager.Instance;
        BusPlayerController player = Object.FindFirstObjectByType<BusPlayerController>();
        if (manager == null || player == null)
            return;

        manager.TryPickUpSpray(this, player);
    }

    public string GetPromptText()
    {
        return "กด E เพื่อหยิบสเปรย์";
    }

    public void SetCarriedState(bool carried)
    {
        if (itemBody != null)
        {
            itemBody.isKinematic = carried;
            itemBody.useGravity = !carried;
            itemBody.linearVelocity = Vector3.zero;
            itemBody.angularVelocity = Vector3.zero;
        }

        if (itemCollider != null)
            itemCollider.enabled = !carried;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        gameObject.layer = ignoreRaycastLayer >= 0 && carried ? ignoreRaycastLayer : 0;

        if (carried)
            playDropSoundOnNextCollision = false;

        if (!carried)
            SetSprayEffectActive(false);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!playDropSoundOnNextCollision || hasPlayedDropSound)
            return;

        if (collision == null || collision.relativeVelocity.magnitude < minDropImpactSpeed)
            return;

        AudioClip clip = GetBoxDropClip();
        if (clip == null)
            return;

        Vector3 soundPosition = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        AudioSource.PlayClipAtPoint(clip, soundPosition, boxDropVolume);
        hasPlayedDropSound = true;
        playDropSoundOnNextCollision = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void PrewarmBoxDropClip()
    {
        GetBoxDropClip();
    }

    static AudioClip GetBoxDropClip()
    {
        if (cachedBoxDropClip == null)
            cachedBoxDropClip = Resources.Load<AudioClip>(BoxDropClipResourcePath);

        if (cachedBoxDropClip != null && !cachedBoxDropClip.preloadAudioData && !cachedBoxDropClip.loadState.Equals(AudioDataLoadState.Loaded))
            cachedBoxDropClip.LoadAudioData();

        return cachedBoxDropClip;
    }

    static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;
        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
            SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
    }

    void EnsureVisualRoot()
    {
        if (visualRoot != null)
            return;

        Transform existing = transform.Find("VisualRoot");
        if (existing != null)
        {
            visualRoot = existing;
            return;
        }

        GameObject visualRootObject = new GameObject("VisualRoot");
        visualRoot = visualRootObject.transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;
    }

    void EnsureSprayEffect()
    {
        if (sprayEffect != null)
            return;

        EnsureVisualRoot();
        if (visualRoot == null)
            return;

        Transform existing = visualRoot.Find("SprayEffect");
        if (existing != null)
        {
            sprayEffect = existing.GetComponent<ParticleSystem>();
            return;
        }

        GameObject effectObject = new GameObject("SprayEffect");
        effectObject.transform.SetParent(visualRoot, false);
        effectObject.transform.localPosition = new Vector3(0f, 0.22f, 0.34f);
        effectObject.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
        effectObject.transform.localScale = Vector3.one;

        sprayEffect = effectObject.AddComponent<ParticleSystem>();
        var main = sprayEffect.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.095f);
        main.startColor = new Color(1f, 0.98f, 0.99f, 0.28f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 140;

        var emission = sprayEffect.emission;
        emission.enabled = true;
        emission.rateOverTime = 78f;

        var shape = sprayEffect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.026f;
        shape.rotation = new Vector3(0f, 0f, 0f);

        var velocityOverLifetime = sprayEffect.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.14f);

        var colorOverLifetime = sprayEffect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.98f, 0.99f), 0f),
                new GradientColorKey(new Color(0.97f, 1f, 0.98f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.22f, 0.1f),
                new GradientAlphaKey(0.08f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = sprayEffect.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.25f, 1f);
        sizeCurve.AddKey(0.7f, 1.3f);
        sizeCurve.AddKey(1f, 1.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var noise = sprayEffect.noise;
        noise.enabled = true;
        noise.strength = 0.42f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.3f;

        var renderer = sprayEffect.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0.001f;
        renderer.maxParticleSize = 0.12f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = GetSprayEffectMaterial();

        sprayEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static Material GetSprayEffectMaterial()
    {
        if (cachedSprayEffectMaterial != null)
            return cachedSprayEffectMaterial;

        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        if (shader == null)
            return null;

        cachedSprayEffectMaterial = new Material(shader)
        {
            name = "RuntimeSprayEffectMaterial"
        };

        Texture2D texture = GetSprayEffectTexture();
        if (texture != null)
        {
            if (cachedSprayEffectMaterial.HasProperty("_BaseMap"))
                cachedSprayEffectMaterial.SetTexture("_BaseMap", texture);

            if (cachedSprayEffectMaterial.HasProperty("_MainTex"))
                cachedSprayEffectMaterial.SetTexture("_MainTex", texture);
        }

        if (cachedSprayEffectMaterial.HasProperty("_BaseColor"))
            cachedSprayEffectMaterial.SetColor("_BaseColor", new Color(1f, 0.98f, 0.99f, 0.32f));

        if (cachedSprayEffectMaterial.HasProperty("_Color"))
            cachedSprayEffectMaterial.SetColor("_Color", new Color(1f, 0.98f, 0.99f, 0.32f));

        if (cachedSprayEffectMaterial.HasProperty("_Mode"))
            cachedSprayEffectMaterial.SetFloat("_Mode", 2f);

        if (cachedSprayEffectMaterial.HasProperty("_SrcBlend"))
            cachedSprayEffectMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (cachedSprayEffectMaterial.HasProperty("_DstBlend"))
            cachedSprayEffectMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (cachedSprayEffectMaterial.HasProperty("_BlendOp"))
            cachedSprayEffectMaterial.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);

        if (cachedSprayEffectMaterial.HasProperty("_Surface"))
            cachedSprayEffectMaterial.SetFloat("_Surface", 1f);

        if (cachedSprayEffectMaterial.HasProperty("_Blend"))
            cachedSprayEffectMaterial.SetFloat("_Blend", 0f);

        if (cachedSprayEffectMaterial.HasProperty("_AlphaClip"))
            cachedSprayEffectMaterial.SetFloat("_AlphaClip", 0f);

        if (cachedSprayEffectMaterial.HasProperty("_ZWrite"))
            cachedSprayEffectMaterial.SetFloat("_ZWrite", 0f);

        if (cachedSprayEffectMaterial.HasProperty("_SoftParticlesEnabled"))
            cachedSprayEffectMaterial.SetFloat("_SoftParticlesEnabled", 0f);

        if (cachedSprayEffectMaterial.HasProperty("_Cutoff"))
            cachedSprayEffectMaterial.SetFloat("_Cutoff", 0f);

        cachedSprayEffectMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return cachedSprayEffectMaterial;
    }

    static Texture2D GetSprayEffectTexture()
    {
        if (cachedSprayEffectTexture != null)
            return cachedSprayEffectTexture;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        texture.name = "RuntimeSprayEffectTexture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalized = Mathf.Clamp01(distance / radius);
                float alpha = Mathf.Pow(1f - normalized, 2.2f);
                texture.SetPixel(x, y, new Color(alpha, alpha, alpha, alpha));
            }
        }

        texture.Apply(false, true);
        cachedSprayEffectTexture = texture;
        return cachedSprayEffectTexture;
    }

    void OnDestroy()
    {
        if (SprayDeliveryManager.Instance != null)
            SprayDeliveryManager.Instance.NotifyItemDestroyed(this);
    }
}
