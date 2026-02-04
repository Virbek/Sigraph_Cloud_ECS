using UnityEngine;

public class CloudVolumeFiller : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private Vector3 volumeSize = new Vector3(10f, 6f, 10f);
    [SerializeField] private Vector3 volumeOffset = Vector3.zero;

    [Header("Emission Settings")]
    [SerializeField] private int emissionRatePerSecond = 50;
    [SerializeField] private float particleLifetime = 5f;

    [Header("Particle Settings")]
    [SerializeField] private float particleSize = 1.5f;
    [SerializeField] private float particleSizeVariation = 0.3f;
    [SerializeField] private Color smokeColor = new Color(0.85f, 0.9f, 1f, 0.25f);

    [Header("Movement")]
    [SerializeField] private float driftSpeed = 0.2f;
    [SerializeField] private float turbulence = 0.5f;
    [SerializeField] private Vector3 windDirection = new Vector3(0.1f, 0.2f, 0f);

    [Header("Visual")]
    [SerializeField] private bool showVolumeBounds = true;

    private ParticleSystem volumeParticles;

    void Start()
    {
        SetupVolumeParticleSystem();
    }

    void SetupVolumeParticleSystem()
    {
        GameObject psObject = new GameObject("VolumeSmoke");
        psObject.transform.parent = transform;
        psObject.transform.localPosition = volumeOffset;

        volumeParticles = psObject.AddComponent<ParticleSystem>();

        // === MAIN ===
        var main = volumeParticles.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = driftSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(
            particleSize * (1 - particleSizeVariation),
            particleSize * (1 + particleSizeVariation)
        );
        main.startColor = smokeColor;
        main.gravityModifier = -0.03f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;
        main.loop = true;
        main.playOnAwake = true;

        // === EMISSION ===
        var emission = volumeParticles.emission;
        emission.rateOverTime = emissionRatePerSecond;

        // === SHAPE - BOX pour remplir tout le volume ===
        var shape = volumeParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = volumeSize;
        shape.randomDirectionAmount = 1f;
        shape.sphericalDirectionAmount = 0f;

        // === COLOR OVER LIFETIME ===
        var colorOverLifetime = volumeParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.3f, 0.1f),
                new GradientAlphaKey(0.4f, 0.3f),
                new GradientAlphaKey(0.5f, 0.6f),
                new GradientAlphaKey(0.3f, 0.85f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // === SIZE OVER LIFETIME - Grossit avec le temps ===
        var sizeOverLifetime = volumeParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;

        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.3f);
        sizeCurve.AddKey(0.2f, 0.8f);
        sizeCurve.AddKey(0.5f, 1.2f);
        sizeCurve.AddKey(0.8f, 1.8f);
        sizeCurve.AddKey(1.0f, 2.2f);

        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // === VELOCITY OVER LIFETIME - Mouvement organique ===
        var velocityOverLifetime = volumeParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;

        // Vent constant
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(windDirection.x);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(windDirection.y);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(windDirection.z);

        // === NOISE - Turbulence ===
        var noise = volumeParticles.noise;
        noise.enabled = true;
        noise.strength = turbulence;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.octaveCount = 2;
        noise.octaveMultiplier = 0.5f;
        noise.octaveScale = 2f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // === ROTATION OVER LIFETIME ===
        var rotationOverLifetime = volumeParticles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-45f, 45f);

        // === RENDERER ===
        var renderer = volumeParticles.GetComponent<ParticleSystemRenderer>();
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

    // Mettre à jour le volume basé sur la grille
    public void UpdateVolumeSize(Vector3 newSize)
    {
        volumeSize = newSize;
        if (volumeParticles != null)
        {
            var shape = volumeParticles.shape;
            shape.scale = volumeSize;
        }
    }

    public void SetEmissionRate(int rate)
    {
        emissionRatePerSecond = rate;
        if (volumeParticles != null)
        {
            var emission = volumeParticles.emission;
            emission.rateOverTime = rate;
        }
    }

    void OnDrawGizmos()
    {
        if (!showVolumeBounds) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(volumeOffset, volumeSize);
    }
}