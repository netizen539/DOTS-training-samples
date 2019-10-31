using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(InertialSystem))]
public class CheckAndResetSystem : JobComponentSystem
{

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbs.CreateCommandBuffer().ToConcurrent();

        var jobHandle = Entities
        .WithName("CheckAndResetSystem")
        .WithAll<InFlightTag>()
        .ForEach(
            (int entityInQueryIndex, Entity e, in ref Translation pos) => 
            {
                if (pos.Value.y < -5f)
                {
                    ecb.AddComponent(entityInQueryIndex, e, new ResetTag());
                    ecb.RemoveComponent<InFlightTag>(entityInQueryIndex, e);
                }
            })
            .Schedule(inputDeps);
        
        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
   }
}