using UnityEngine;

public class CloudVolumeInfluencer : MonoBehaviour
{
    [SerializeField] private CloudMeshController meshController;
    [SerializeField] private ParticleSystem cloudParticles;
    [SerializeField] private float influenceRadius = 3f;
    [SerializeField] private float influenceStrength = 1f;

    private ParticleSystem.Particle[] particles;

    void Start()
    {
        if (cloudParticles == null)
            cloudParticles = GetComponent<ParticleSystem>();

        particles = new ParticleSystem.Particle[cloudParticles.main.maxParticles];
    }

    void LateUpdate()
    {
        if (cloudParticles == null) return;

        int particleCount = cloudParticles.GetParticles(particles);

        // Influencer les particules selon les points de contrôle
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 particlePos = particles[i].position;
            Vector3 influence = Vector3.zero;

            // Trouver les points de contrôle proches
            // (Cette partie nécessiterait d'accéder aux controlPoints)
            // Pour l'instant, simple déplacement basé sur la position

            particles[i].position += influence * influenceStrength * Time.deltaTime;
        }

        cloudParticles.SetParticles(particles, particleCount);
    }
}