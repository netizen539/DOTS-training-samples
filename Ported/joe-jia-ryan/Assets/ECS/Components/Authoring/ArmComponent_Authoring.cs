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

    private void PopulateBuffers(Entity e, EntityManager entityManager)
    {
        DynamicBuffer<ArmChainsBuffer> armChains = entityManager.GetBuffer<ArmChainsBuffer>(e);
        for (int i = 0; i < armChains.Capacity; i++)
            armChains.Add(new ArmChainsBuffer() {element = new float3()});
        
        DynamicBuffer<FingerChainsBuffer> fingerChains = entityManager.GetBuffer<FingerChainsBuffer>(e);
        for (int i = 0; i < fingerChains.Capacity; i++)
            fingerChains.Add(new FingerChainsBuffer() {element = new float3()});
        
        DynamicBuffer<ThumbChainsBuffer> thumbChains = entityManager.GetBuffer<ThumbChainsBuffer>(e);
        for (int i = 0; i < thumbChains.Capacity; i++)
            thumbChains.Add(new ThumbChainsBuffer() {element = new float3()});

    }
    
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
        dstManager.AddBuffer<ArmChainsBuffer>(entity);
        dstManager.AddBuffer<FingerChainsBuffer>(entity);
        dstManager.AddBuffer<ThumbChainsBuffer>(entity);
        PopulateBuffers(entity, dstManager);

    }
}
