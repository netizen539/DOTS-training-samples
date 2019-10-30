using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public class RandomRockStateTransitionSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbs.CreateCommandBuffer().ToConcurrent();
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random();
        rnd.InitState();
        
        var jobHandle = inputDeps;

        jobHandle = Entities
        .WithName("MoveConveyorToHeld")
        .WithAll<RockComponent, ConveyorComponent>()
        .ForEach(
            (int entityInQueryIndex, Entity e) => 
            {
                float val = rnd.NextFloat(0f, 1f);
                if (val > 0.95f)
                {
                    ecb.RemoveComponent<ConveyorComponent>(entityInQueryIndex, e);
                    ecb.AddComponent(entityInQueryIndex, e, new RockHeldComponent{Velocity = new float3(0f,0f,-1.0f)});
                }
            })
            .Schedule(jobHandle);


        jobHandle = Entities
        .WithName("MoveHeldToThrown")
        .WithAll<RockComponent, RockHeldComponent>()
        .ForEach(
            (int entityInQueryIndex, Entity e) => 
            {
                float val = rnd.NextFloat(0f, 1f);
                if (val > 0.9f)
                {
                    ecb.RemoveComponent<RockHeldComponent>(entityInQueryIndex, e);
                    ecb.AddComponent(entityInQueryIndex, e, new RockThrownComponent{ Velocity = new float3(0f, 2f, 2f)});
                }
            })
            .Schedule(jobHandle);

        // jobHandle = Entities
        // .WithName("MoveThrownToInFlight")
        // .WithAll<RockComponent>()
        // .ForEach(
        //     (int entityInQueryIndex, Entity e, ref RockThrownComponent rtc, ref RigidBodyComponent rbc) => 
        //     {
        //         float val = rnd.NextFloat(0f, 1f);
        //         if (val > 0.5f)
        //         {
        //             ecb.RemoveComponent<RockThrownComponent>(entityInQueryIndex, e);
        //             rbc.Velocity = rtc.Velocity;
        //             ecb.AddComponent(entityInQueryIndex, e, new InFlightTag());
        //         }
        //     })
        //     .Schedule(jobHandle);

        jobHandle = Entities
        .WithName("MoveInFlightToReset")
        .WithAll<RockComponent, InFlightTag>()
        .ForEach(
            (int entityInQueryIndex, Entity e) => 
            {
                float val = rnd.NextFloat(0f, 1f);
                if (val > 0.5f)
                {
                    ecb.RemoveComponent<InFlightTag>(entityInQueryIndex, e);
                    ecb.AddComponent(entityInQueryIndex, e, new ResetTag());
                }
            })
            .Schedule(jobHandle);

        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}