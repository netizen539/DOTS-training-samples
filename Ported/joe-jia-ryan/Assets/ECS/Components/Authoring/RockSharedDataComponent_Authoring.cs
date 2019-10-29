using UnityEngine;

using Unity.Entities;

public class RockSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new RockSharedDataComponent{};
        dstManager.SetSharedComponentData(entity, data);
    }
}