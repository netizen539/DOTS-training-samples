using UnityEngine;

using Unity.Entities;

[RequiresEntityConversion]
public class ThrowingArmsSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public int ArmCount;
    public float ArmWidth;
    public float ConveyorMargin;
    public float ConveyorSpeed;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        float conveyorWidth = this.ArmCount * this.ArmWidth;
        var data = new ThrowingArmsSharedDataComponent
        {
            ArmCount = this.ArmCount,
            ArmWidth = this.ArmWidth,
            ConveyorMargin = this.ConveyorMargin,
            ConveyorWidth = conveyorWidth,
            ConveyorMaxX = 0,
            ConveyorMinX = conveyorWidth + this.ConveyorMargin,
            ConveyorSpeed = this.ConveyorSpeed,
        };
        dstManager.AddComponentData(entity, new SpawnAllComponent());
        dstManager.AddComponentData(entity, data);
    }
}