using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ConveyorSystems))]
public class RockHeldSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer().ToConcurrent();
        float deltaTime = Time.deltaTime;
        var jobHandle = inputDeps;

        jobHandle = Entities
        .WithName("RockHeldSystem")
        .WithAll<RockComponent>()
        .ForEach(
            (int entityInQueryIndex, Entity e, ref RockHeldComponent rhc, ref Translation pos) =>
            {
                pos.Value = rhc.rockInHandPosition;
            })
            .Schedule(jobHandle);

 
        return jobHandle;
    }
}