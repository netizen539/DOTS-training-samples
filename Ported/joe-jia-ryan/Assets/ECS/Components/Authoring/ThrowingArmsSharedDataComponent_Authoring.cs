using UnityEngine;

using Unity.Entities;

public class ThrowingArmsSharedDataComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new ThrowingArmsSharedDataComponent{};
        dstManager.AddSharedComponentData(entity, data);
    }
}