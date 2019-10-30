using Unity.Entities;

[System.Serializable]
public struct TinCanSharedDataComponent : ISharedComponentData
{
    public float MinHeightFromGround;
    public float MaxHeightFromGround;
    public float SizeGrowthFactor;
    public float Gravity;

}