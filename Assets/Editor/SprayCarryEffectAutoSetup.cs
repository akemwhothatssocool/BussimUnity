using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class SprayCarryEffectAutoSetup
{
    const string AnchorName = "SprayCarryAnchor";
    const string EffectName = "SprayCarryEffect";
    const string GeneratedRootFolder = "Assets/Generated";
    const string GeneratedFolder = "Assets/Generated/SprayEffects";
    const string TextureAssetPath = GeneratedFolder + "/SprayCarryDot.png";
    const string MaterialAssetPath = GeneratedFolder + "/SprayCarryDot.mat";

    static SprayCarryEffectAutoSetup()
    {
        EditorApplication.delayCall += EnsureAllAnchorsHaveEffect;
    }

    [MenuItem("Tools/Bus Sim/Create Spray Carry Effect")]
    public static void EnsureAllAnchorsHaveEffect()
    {
        if (Application.isPlaying)
            return;

        bool changedAnyScene = false;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    Transform candidate = transforms[transformIndex];
                    if (candidate == null || candidate.name != AnchorName)
                        continue;

                    CreateEffect(candidate);
                    changedAnyScene = true;
                }
            }

            if (changedAnyScene)
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    static void CreateEffect(Transform anchor)
    {
        Transform existing = anchor.Find(EffectName);
        GameObject effectObject = existing != null ? existing.gameObject : new GameObject(EffectName);
        effectObject.transform.SetParent(anchor, false);
        effectObject.transform.localPosition = new Vector3(0f, 0.22f, 0.34f);
        effectObject.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
        effectObject.transform.localScale = Vector3.one;

        ParticleSystem particleSystem = effectObject.GetComponent<ParticleSystem>();
        if (particleSystem == null)
            particleSystem = effectObject.AddComponent<ParticleSystem>();

        ParticleSystemRenderer renderer = effectObject.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
            renderer = effectObject.AddComponent<ParticleSystemRenderer>();

        var main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.8f, 6.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.085f, 0.15f);
        main.startColor = new Color(1f, 0.98f, 0.99f, 0.6f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 340;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 190f;

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 0.03f;

        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(1.15f, 2.05f);
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.22f);

        var colorOverLifetime = particleSystem.colorOverLifetime;
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
                new GradientAlphaKey(0.7f, 0.06f),
                new GradientAlphaKey(0.34f, 0.76f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.85f);
        sizeCurve.AddKey(0.22f, 1.08f);
        sizeCurve.AddKey(0.68f, 1.5f);
        sizeCurve.AddKey(1f, 1.82f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.26f;
        noise.frequency = 0.52f;
        noise.scrollSpeed = 0.22f;

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0.001f;
        renderer.maxParticleSize = 0.22f;
        renderer.sharedMaterial = GetOrCreateStylizedParticleMaterial();

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static Material GetOrCreateStylizedParticleMaterial()
    {
        EnsureGeneratedFolders();

        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
        if (existingMaterial != null)
            return existingMaterial;

        Texture2D texture = GetOrCreateStylizedParticleTexture();
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

        Material material = new Material(shader)
        {
            name = "SprayCarryDot"
        };

        if (texture != null)
        {
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        AssetDatabase.CreateAsset(material, MaterialAssetPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    static Texture2D GetOrCreateStylizedParticleTexture()
    {
        Texture2D existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        if (existingTexture != null)
            return existingTexture;

        EnsureGeneratedFolders();

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "SprayCarryDot";

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float innerRadius = size * 0.31f;
        float outerRadius = size * 0.35f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance <= innerRadius ? 1f : distance >= outerRadius ? 0f : 0.35f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        byte[] pngBytes = texture.EncodeToPNG();
        File.WriteAllBytes(TextureAssetPath, pngBytes);
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(TextureAssetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(TextureAssetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
    }

    static void EnsureGeneratedFolders()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedRootFolder))
            AssetDatabase.CreateFolder("Assets", "Generated");

        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            AssetDatabase.CreateFolder(GeneratedRootFolder, "SprayEffects");
    }
}
