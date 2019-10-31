using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public class RandomRockStateTransitionSystem : JobComponentSystem
{
    int count = 0;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
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
        .ForEach(
            (int entityInQueryIndex, Entity e) => 
            {
                int val = rnd.NextInt(0, 1000);
                if (c % 1000 == val)
                {
                    ecb.RemoveComponent<ConveyorComponent>(entityInQueryIndex, e);
                    ecb.AddComponent(entityInQueryIndex, e, new RockHeldComponent{Velocity = new float3(0f,0f,-10f)});
                }
            })
            .Schedule(jobHandle);


        jobHandle = Entities
        .WithName("MoveHeldToThrown")
        .WithAll<RockComponent, RockHeldComponent>()
        .ForEach(
            (int entityInQueryIndex, Entity e) => 
            {
                ecb.RemoveComponent<RockHeldComponent>(entityInQueryIndex, e);
                ecb.AddComponent(entityInQueryIndex, e, new RockThrownComponent{ Velocity = new float3(0f, rnd.NextFloat(5f, 20f), rnd.NextFloat(10f, 40f))});
            })
            .Schedule(jobHandle);

        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}