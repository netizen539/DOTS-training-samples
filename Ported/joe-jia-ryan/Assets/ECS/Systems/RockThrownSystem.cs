using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public class RockThrownSystem : JobComponentSystem
{

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbs.CreateCommandBuffer().ToConcurrent();

        var jobHandle = Entities
        .WithName("RockThrowSystem")
        .ForEach(
            (int entityInQueryIndex, Entity e, ref RockThrownComponent rhc, ref RigidBodyComponent rbc) => 
            {
                rbc.Velocity = rhc.Velocity;
                ecb.AddComponent(entityInQueryIndex, e, new InFlightTag());
                ecb.RemoveComponent<RockThrownComponent>(entityInQueryIndex, e);
            })
            .Schedule(inputDeps);
        
        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
   }
}