using Unity.Entities;

[System.Serializable]
public struct ThrowingArmsSharedDataComponent : IComponentData
{
    public int ArmCount;
    public float ArmWidth;
    public float ConveyorMargin;
    public float ConveyorWidth;
    public float ConveyorMaxX;
    public float ConveyorMinX;
    public float ConveyorSpeed;
}