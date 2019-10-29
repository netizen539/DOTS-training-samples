using Unity.Entities;

public struct TinCanSharedDataComponent : ISharedComponentData
{
    public Entity Prefab;
    public float MinHeightFromGround;
    public float MaxHeightFromGround;
    public float SizeGrowthFactor;
    public float Gravity;

}