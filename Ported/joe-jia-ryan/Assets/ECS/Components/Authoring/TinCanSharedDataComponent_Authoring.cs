using UnityEngine;

using Unity.Entities;

public class TinCanSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new TinCanSharedDataComponent{};
        dstManager.SetSharedComponentData(entity, data);
    }
}