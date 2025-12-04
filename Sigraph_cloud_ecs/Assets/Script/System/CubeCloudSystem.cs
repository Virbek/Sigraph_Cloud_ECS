using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;


[BurstCompile]
public partial struct CubeCloudSystem : ISystem
{
    // On doit initialiser le générateur de nombres aléatoires
    private Random _random;

    public void OnCreate(ref SystemState state)
    {
        // RequireForUpdate force le système à attendre que le component existe
        state.RequireForUpdate<CubeCloudConfig>();
        _random = new Random(1234); // Seed arbitraire
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        // Récupérer la config (Singleton car on suppose un seul spawner pour l'exemple)
        var config = SystemAPI.GetSingleton<CubeCloudConfig>();

        // Création d'un tableau d'entités
        var instances = state.EntityManager.Instantiate(config.CubePrefab, config.CubeCount, Allocator.Temp);

        // Boucle pour positionner chaque cube
        foreach (var entity in instances)
        {
            // Position aléatoire dans une sphère
            float3 randomPos = _random.NextFloat3Direction() * _random.NextFloat(0, config.SpawnRadius);

            // ECS utilise LocalTransform pour la position/rotation/échelle
            var transform = LocalTransform.FromPosition(randomPos);

            // Optionnel : Ajouter une rotation aléatoire
            transform.Rotation = _random.NextQuaternionRotation();

            // Appliquer la transformation
            state.EntityManager.SetComponentData(entity, transform);
        }
    }
}