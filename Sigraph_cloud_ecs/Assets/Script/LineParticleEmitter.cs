using UnityEngine;

public class LineParticleEmitter : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int particlesPerLine = 15;
    [SerializeField] private float emissionInterval = 0.08f;
    [SerializeField] private float particleLifetime = 4f;

    [Header("Spread Settings")]
    [SerializeField] private float radialSpread = 1.5f; // Dispersion radiale autour de la ligne
    [SerializeField] private float randomSpread = 0.8f; // Dispersion aléatoire
    [SerializeField] private float tubeDiameter = 1.2f; // Diamètre du "tube" de fumée

    [Header("Visual")]
    [SerializeField] private float particleSize = 1.2f;
    [SerializeField] private float particleSizeVariation = 0.5f;
    [SerializeField] private Color smokeColor = new Color(0.9f, 0.95f, 1f, 0.3f);

    [Header("Movement")]
    [SerializeField] private float driftSpeed = 0.3f;
    [SerializeField] private float expansionSpeed = 0.5f;

    private LineRenderer lineRenderer;
    private ParticleSystem particleSystem;
    private float emissionTimer = 0f;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupParticleSystem();
    }

    void SetupParticleSystem()
    {
        GameObject psObject = new GameObject("LineParticles");
        psObject.transform.parent = transform;
        psObject.transform.localPosition = Vector3.zero;

        particleSystem = psObject.AddComponent<ParticleSystem>();

        var main = particleSystem.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = driftSpeed;
        main.startSize = particleSize;
        main.startColor = smokeColor;
        main.gravityModifier = -0.02f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.loop = false;
        main.playOnAwake = false;

        var emission = particleSystem.emission;
        emission.enabled = false;

        var shape = particleSystem.shape;
        shape.enabled = false;

        // Couleur qui s'estompe
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.4f, 0.15f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0.3f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Taille qui augmente (fumée qui s'étale)
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;

        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.4f);
        curve.AddKey(0.3f, 1.0f);
        curve.AddKey(0.7f, 1.8f);
        curve.AddKey(1.0f, 2.5f);

        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Vélocité pour expansion
        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(expansionSpeed);

        // Rotation
        var rotationOverLifetime = particleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-30f, 30f);

        // Renderer
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

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

    void Update()
    {
        if (lineRenderer == null || particleSystem == null) return;

        emissionTimer += Time.deltaTime;

        if (emissionTimer >= emissionInterval)
        {
            emissionTimer = 0f;
            EmitParticlesAlongLine();
        }
    }

    void EmitParticlesAlongLine()
    {
        if (lineRenderer.positionCount < 2) return;

        Vector3 startPos = lineRenderer.GetPosition(0);
        Vector3 endPos = lineRenderer.GetPosition(1);
        Vector3 lineDirection = (endPos - startPos).normalized;

        // Calculer un vecteur perpendiculaire pour la dispersion radiale
        Vector3 perpendicular1 = Vector3.Cross(lineDirection, Vector3.up).normalized;
        if (perpendicular1.magnitude < 0.1f) // Si la ligne est verticale
            perpendicular1 = Vector3.Cross(lineDirection, Vector3.forward).normalized;

        Vector3 perpendicular2 = Vector3.Cross(lineDirection, perpendicular1).normalized;

        // Émettre des particules le long de la ligne
        for (int i = 0; i < particlesPerLine; i++)
        {
            float t = i / (float)(particlesPerLine - 1);
            Vector3 basePosition = Vector3.Lerp(startPos, endPos, t);

            // Créer plusieurs particules autour de chaque point de la ligne
            int radialCount = 3; // Nombre de particules autour de chaque point
            for (int r = 0; r < radialCount; r++)
            {
                float angle = r * (360f / radialCount) + Random.Range(0f, 60f);
                float distance = Random.Range(0f, radialSpread);

                // Position radiale autour de la ligne
                Vector3 radialOffset =
                    (perpendicular1 * Mathf.Cos(angle * Mathf.Deg2Rad) +
                     perpendicular2 * Mathf.Sin(angle * Mathf.Deg2Rad)) * distance;

                // Ajouter variation aléatoire
                Vector3 randomOffset = Random.insideUnitSphere * randomSpread;

                Vector3 finalPosition = basePosition + radialOffset + randomOffset;

                EmitParticle(finalPosition, perpendicular1, perpendicular2);
            }
        }
    }

    void EmitParticle(Vector3 position, Vector3 perp1, Vector3 perp2)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = position;

        // Vélocité radiale pour expansion
        float angle = Random.Range(0f, 360f);
        Vector3 radialVelocity =
            (perp1 * Mathf.Cos(angle * Mathf.Deg2Rad) +
             perp2 * Mathf.Sin(angle * Mathf.Deg2Rad)) * Random.Range(0.1f, 0.4f);

        Vector3 randomVelocity = Random.insideUnitSphere * 0.2f;
        emitParams.velocity = radialVelocity + randomVelocity;

        emitParams.startSize = particleSize * Random.Range(1f - particleSizeVariation, 1f + particleSizeVariation);
        emitParams.startLifetime = particleLifetime * Random.Range(0.8f, 1.2f);
        emitParams.startColor = smokeColor;

        particleSystem.Emit(emitParams, 1);
    }
}