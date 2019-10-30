using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class RockDebug_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    // Add fields to your component here. Remember that:
    //
    // * The purpose of this class is to store data for authoring purposes - it is not for use while the game is
    //   running.
    // 
    // * Traditional Unity serialization rules apply: fields must be public or marked with [SerializeField], and
    //   must be one of the supported types.
    //
    // For example,
    //    public float scale;

    public bool addConvyer = true;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new RockComponent());
        if (addConvyer)
        {
            dstManager.AddComponentData(entity, new ConvoyorComponent());
        }
    }
}
