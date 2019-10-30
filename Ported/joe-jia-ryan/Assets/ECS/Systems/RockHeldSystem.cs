using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public class RockHeldSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer().ToConcurrent();
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random();
        rnd.InitState();
        
        float deltaTime = Time.deltaTime;

        var jobHandle = inputDeps;

        jobHandle = Entities
        .WithName("MoveHeldRock")
        .WithAll<RockComponent>()
        .ForEach(
            (int entityInQueryIndex, Entity e, ref RockHeldComponent rhc, ref Translation pos) => 
            {
                pos.Value += deltaTime * rhc.Velocity;
            })
            .Schedule(jobHandle);

 
        return jobHandle;
    }
}