using System.Collections.Generic;

using UnityEngine;

using Unity.Entities;

[RequiresEntityConversion]
public class TinCanSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MinHeightFromGround;
    public float MaxHeightFromGround;
    public float SizeGrowthFactor;
    public float Gravity;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new TinCanSharedDataComponent
        {
            MinHeightFromGround = this.MinHeightFromGround,
            MaxHeightFromGround = this.MaxHeightFromGround,
            SizeGrowthFactor = this.SizeGrowthFactor,
            Gravity = this.Gravity
        };

        dstManager.AddSharedComponentData(entity, data);
    }

}