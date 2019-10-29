using Unity.Entities;

[System.Serializable]
public struct RockSharedDataComponent : ISharedComponentData
{
    public Entity Prefab;
    public float MinRockSize;
    public float MaxRockSize;
    public float SizeGrowthFactor;
    public float Gravity;
}