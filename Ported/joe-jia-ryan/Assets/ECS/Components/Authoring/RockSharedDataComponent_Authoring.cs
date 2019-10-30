using System.Collections.Generic;

using UnityEngine;

using Unity.Entities;

[RequiresEntityConversion]
public class RockSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public ThrowingArmsSharedDataComponent_Authoring throwingArmsSharedDataComponent;
    public GameObject Prefab;
    public float MinRockSize;
    public float MaxRockSize;
    public float SizeGrowthFactor;
    public float Gravity;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var count = throwingArmsSharedDataComponent.ArmCount;
        var spawn = new SpawnAllComponent{ Prefab = conversionSystem.GetPrimaryEntity(Prefab), Count = count};
        dstManager.AddComponentData(entity, spawn);

        var data = new RockSharedDataComponent
        {
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