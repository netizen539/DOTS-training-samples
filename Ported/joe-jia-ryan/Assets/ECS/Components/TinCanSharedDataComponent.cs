using Unity.Entities;

[System.Serializable]
public struct TinCanSharedDataComponent : IComponentData
{
    public float MinHeightFromGround;
    public float MaxHeightFromGround;
    public float SizeGrowthFactor;
    public float Gravity;

}