using Unity.Entities;
using Unity.Mathematics;

public struct CubeCloudConfig : IComponentData
{
    public Entity CubePrefab; // Le modèle de cube à copier
    public int CubeCount;     // Nombre de cubes
    public float SpawnRadius; // Rayon du nuage
}