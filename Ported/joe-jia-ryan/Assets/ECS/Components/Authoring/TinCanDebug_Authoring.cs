using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class TinCanDebug_Authoring : MonoBehaviour, IConvertGameObjectToEntity
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
    
    

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TinCanComponent());
        RigidBodyComponent rigidBodyComponent = new RigidBodyComponent();
        rigidBodyComponent.Gravity = 25f;
        rigidBodyComponent.Velocity = 0f;
        rigidBodyComponent.AngularVelocity = 0f;
        dstManager.AddComponentData(entity, rigidBodyComponent);

    }
}
