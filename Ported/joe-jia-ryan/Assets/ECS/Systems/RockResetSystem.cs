using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CheckAndResetSystem))]
public class RockResetSystem : JobComponentSystem
{
    uint frameCount = 0;
    EntityQuery m_InitDataQuery;

    protected override void OnCreate()
    {
        m_InitDataQuery = GetEntityQuery(typeof(ThrowingArmsSharedDataComponent), typeof(RockSharedDataComponent));
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        frameCount++;

        var throwingArmsComponentArray = m_InitDataQuery.ToComponentDataArray<ThrowingArmsSharedDataComponent>(Allocator.TempJob);
        var rockComponentArray = m_InitDataQuery.ToComponentDataArray<RockSharedDataComponent>(Allocator.TempJob);
        var ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbs.CreateCommandBuffer().ToConcurrent();
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random();
        rnd.InitState(frameCount);
        var right = new float3(1.0f, 0f, 0f);

        var jobHandle = Entities
        .WithName("RockResetSystem")
        .WithAll<RockComponent, ResetTag>()
        .WithDeallocateOnJobCompletion(throwingArmsComponentArray)
        .WithDeallocateOnJobCompletion(rockComponentArray)
        .ForEach(
            (int entityInQueryIndex, Entity e, ref Scale scale, ref Translation pos, ref SizeableComponent sc, ref RigidBodyComponent rbc) => 
            {

                var tac = throwingArmsComponentArray[0];
                var rc = rockComponentArray[0];

                sc.CurrentSize = 0;
                scale.Value = 0;
                var minConveyorX = tac.ConveyorMinX;
                var maxConveyorX = tac.ConveyorMaxX;
                pos.Value = new float3(minConveyorX,0f,1.5f);

                ecb.RemoveComponent<InFlightTag>(entityInQueryIndex, e);

                ecb.AddComponent(entityInQueryIndex, e, new ConveyorComponent
                {
                    Speed = tac.ConveyorSpeed,
                    Direction = right,
                    ResetX = minConveyorX,
                    MaxX = maxConveyorX
                });

                rbc.Velocity = new float3(0f,0f,0f);
                ecb.RemoveComponent<ResetTag>(entityInQueryIndex, e);
            })
            .Schedule(inputDeps);
        
        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
   }
}