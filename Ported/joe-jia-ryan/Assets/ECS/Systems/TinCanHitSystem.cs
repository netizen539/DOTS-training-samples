using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CheckHitCanSystem))]
public class TinCanHitSystem : JobComponentSystem
{

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbs.CreateCommandBuffer().ToConcurrent();

        var jobHandle = Entities
        .WithName("CheckAndResetSystem")
        .WithAll<TinCanComponent, TinCanHitTag>()
        .ForEach(
            (int entityInQueryIndex, Entity e) => 
            {
                ecb.RemoveComponent<TinCanHitTag>(entityInQueryIndex, e);
                ecb.AddComponent(entityInQueryIndex, e, new ResetTag());
            })
            .Schedule(inputDeps);
        
        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
   }
}