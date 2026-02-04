using UnityEngine;

public class ControlPointSmoke : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private float emissionRate = 1f;
    [SerializeField] private float particleLifetime = 1f;
    [SerializeField] private float particleSize = 1.2f;
    [SerializeField] private Color smokeColor = new Color(1f, 1f, 1f, 0.35f);

    [Header("Spread Settings")]
    [SerializeField] private float spreadRadius = 1.0f;
    [SerializeField] private float spreadSpeed = 0.8f;
    [SerializeField] private float upwardForce = 0.2f;
    [SerializeField] private float turbulence = 0.7f;

    private ParticleSystem smokeParticles;

    void Start()
    {
        SetupParticleSystem();
    }

    void SetupParticleSystem()
    {
        GameObject psObject = new GameObject("Smoke");
        psObject.transform.parent = transform;
        psObject.transform.localPosition = Vector3.zero;

        smokeParticles = psObject.AddComponent<ParticleSystem>();

        // === MAIN MODULE ===
        var main = smokeParticles.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = spreadSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.7f, particleSize * 1.3f);
        main.startColor = smokeColor;
        main.gravityModifier = -upwardForce;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.loop = true;
        main.playOnAwake = true;

        // === EMISSION ===
        var emission = smokeParticles.emission;
        emission.rateOverTime = emissionRate;

        // === SHAPE - Sphère pour dispersion omnidirectionnelle ===
        var shape = smokeParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spreadRadius;
        shape.radiusThickness = 0.5f; // Émet depuis l'intérieur de la sphère
        shape.randomDirectionAmount = 1f; // Direction complètement aléatoire

        // === COLOR OVER LIFETIME ===
        var colorOverLifetime = smokeParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.5f, 0.15f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0.4f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // === SIZE OVER LIFETIME - Expansion massive ===
        var sizeOverLifetime = smokeParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;

        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.3f);
        sizeCurve.AddKey(0.3f, 1.0f);
        sizeCurve.AddKey(0.6f, 1.8f);
        sizeCurve.AddKey(1.0f, 2.5f);

        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // === VELOCITY OVER LIFETIME ===
        var velocityOverLifetime = smokeParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.3f); // Expansion radiale

        // Mouvement aléatoire dans toutes les directions
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0.5f); // Légèrement vers le haut
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        // === NOISE - Turbulence pour mouvement organique ===
        var noise = smokeParticles.noise;
        noise.enabled = true;
        noise.strength = turbulence;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;
        noise.octaveCount = 2;
        noise.octaveMultiplier = 0.5f;
        noise.octaveScale = 2f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // === ROTATION OVER LIFETIME ===
        var rotationOverLifetime = smokeParticles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-90f, 90f);

        // === RENDERER ===
        var renderer = smokeParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 0;

        Material smokeMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        smokeMaterial.SetColor("_Color", smokeColor);
        smokeMaterial.SetFloat("_Mode", 2);
        smokeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        smokeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        smokeMaterial.SetInt("_ZWrite", 0);
        smokeMaterial.DisableKeyword("_ALPHATEST_ON");
        smokeMaterial.EnableKeyword("_ALPHABLEND_ON");
        smokeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        smokeMaterial.renderQueue = 3000;

        renderer.material = smokeMaterial;
    }

    public void SetEmissionEnabled(bool enabled)
    {
        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.enabled = enabled;
        }
    }

    public void SetEmissionRate(float rate)
    {
        emissionRate = rate;
        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.rateOverTime = rate;
        }
    }
}