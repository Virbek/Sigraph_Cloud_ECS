using Unity.Entities;
using UnityEngine;

public class CubeCloudAuthoring : MonoBehaviour
{
    public GameObject CubePrefab;
    public int CubeCount = 1000;
    public float SpawnRadius = 50f;

    public class Baker : Baker<CubeCloudAuthoring>
    {
        public override void Bake(CubeCloudAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new CubeCloudConfig
            {
                // Convertit le prefab GameObject en prefab Entity
                CubePrefab = GetEntity(authoring.CubePrefab, TransformUsageFlags.Dynamic),
                CubeCount = authoring.CubeCount,
                SpawnRadius = authoring.SpawnRadius
            });
        }
    }
}