using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class ArmComponent_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float maxReachLength = 1.8f;
    public float reachDuration = 1.0f;
    public float armBoneLength = 1.0f;
    public float armBendStrength = 0.1f;
    public float armBoneThickness = 0.15f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new ArmComponent()
        {
            maxReachLength = maxReachLength,
            reachDuration = reachDuration,
            armBoneLength =  armBoneLength,
            handUp = new float3(),
            armBendStrength = armBendStrength,
            timeOffset = Random.value * 100f,
            armBoneThickness = armBoneThickness,
            targetCan = Entity.Null,
            heldRock = Entity.Null,
            windupDuration = 0.7f,
            throwDuration = 1.2f,
        };
        
        dstManager.AddComponentData(entity, data);
        dstManager.AddComponentData(entity, new ArmIdleTag());
        dstManager.AddBuffer<ArmMatrixBuffer>(entity);

    }
}
