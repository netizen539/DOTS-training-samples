using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(RockResetSystem))]
public class RandomRockStateTransitionSystem : JobComponentSystem
{
    EntityQuery m_DataQuery;

    protected override void OnCreate()
    {
        m_DataQuery = GetEntityQuery(typeof(ThrowingArmsSharedDataComponent));
    }

    int count = 0;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var throwingArmsComponentArray = m_DataQuery.ToComponentDataArray<ThrowingArmsSharedDataComponent>(Allocator.TempJob);
        var ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbs.CreateCommandBuffer().ToConcurrent();
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random();
        rnd.InitState();
        
        var jobHandle = inputDeps;
        int c = count;
        count++;

        jobHandle = Entities
        .WithName("MoveConveyorToHeld")
        .WithAll<RockComponent, ConveyorComponent>()
        .WithDeallocateOnJobCompletion(throwingArmsComponentArray)
        .ForEach(
            (int entityInQueryIndex, Entity e, in ref Translation pos) => 
            {
                var tac = throwingArmsComponentArray[0];
                if (pos.Value.x >= 10f)
                {                    
                    int val = rnd.NextInt(0, tac.ArmCount);
                    if (entityInQueryIndex == val)
                    {
                        ecb.RemoveComponent<ConveyorComponent>(entityInQueryIndex, e);
                        ecb.AddComponent(entityInQueryIndex, e, new RockHeldComponent{Velocity = new float3(0f,2f,-2f)});
                    }
                }
            })
            .Schedule(jobHandle);


        jobHandle = Entities
        .WithName("MoveHeldToThrown")
        .WithAll<RockComponent, RockHeldComponent>()
        .ForEach(
            (int entityInQueryIndex, Entity e, in ref Translation pos) => 
            {
                if (pos.Value.y > 1.5f)
                {
                    ecb.RemoveComponent<RockHeldComponent>(entityInQueryIndex, e);
                    ecb.AddComponent(entityInQueryIndex, e, new RockThrownComponent{ Velocity = new float3(0f, rnd.NextFloat(5f, 20f), rnd.NextFloat(10f, 40f))});
                }
            })
            .Schedule(jobHandle);

        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}