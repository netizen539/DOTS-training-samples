using System.Collections.Generic;

using UnityEngine;

using Unity.Entities;

public class RockSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject Prefab;
    public float MinRockSize;
    public float MaxRockSize;
    public float SizeGrowthFactor;
    public float Gravity;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new RockSharedDataComponent
        {
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            MinRockSize = this.MinRockSize,
            MaxRockSize = this.MaxRockSize,
            SizeGrowthFactor = this.SizeGrowthFactor,
            Gravity = this.Gravity
        };

        dstManager.AddSharedComponentData(entity, data);
    }

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(Prefab);
    }
}