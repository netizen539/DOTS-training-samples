using System.Collections.Generic;

using UnityEngine;

using Unity.Entities;

[RequiresEntityConversion]
public class TinCanSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject Prefab;
    public float MinHeightFromGround;
    public float MaxHeightFromGround;
    public float SizeGrowthFactor;
    public float Gravity;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new TinCanSharedDataComponent
        {
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            MinHeightFromGround = this.MinHeightFromGround,
            MaxHeightFromGround = this.MaxHeightFromGround,
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