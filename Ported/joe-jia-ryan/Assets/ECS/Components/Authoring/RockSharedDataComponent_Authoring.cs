using System.Collections.Generic;

using UnityEngine;

using Unity.Entities;

[RequiresEntityConversion]
public class RockSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MinRockSize;
    public float MaxRockSize;
    public float SizeGrowthFactor;
    public float Gravity;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new RockSharedDataComponent
        {
            MinRockSize = this.MinRockSize,
            MaxRockSize = this.MaxRockSize,
            SizeGrowthFactor = this.SizeGrowthFactor,
            Gravity = this.Gravity
        };

        dstManager.AddSharedComponentData(entity, data);
    }

}