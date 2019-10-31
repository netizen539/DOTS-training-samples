using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;

[RequiresEntityConversion]
public class ThrowingArmsSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject RockPrefab;
    public GameObject TinCanPrefab;
    public int ArmCount;
    public float ArmWidth;
    public float ConveyorMargin;
    public float ConveyorSpeed;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var count = this.ArmCount;
        var spawn = new SpawnAllComponent{ 
            RockPrefab = conversionSystem.GetPrimaryEntity(this.RockPrefab),
            TinCanPrefab = conversionSystem.GetPrimaryEntity(this.TinCanPrefab),
            Count = count
        };
        dstManager.AddComponentData(entity, spawn);

        float conveyorWidth = (2.0f * this.ConveyorMargin) + this.ArmCount * this.ArmWidth;
        var data = new ThrowingArmsSharedDataComponent
        {
            ArmCount = this.ArmCount,
            ArmWidth = this.ArmWidth,
            ConveyorMargin = this.ConveyorMargin,
            ConveyorWidth = conveyorWidth,
            ConveyorMaxX = conveyorWidth + this.ConveyorMargin,
            ConveyorMinX = -this.ConveyorMargin,
            ConveyorSpeed = this.ConveyorSpeed,
        };

        dstManager.AddComponentData(entity, data);
    }
    
    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(RockPrefab);
        referencedPrefabs.Add(TinCanPrefab);
    }

}