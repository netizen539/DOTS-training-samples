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
        var ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer().ToConcurrent();
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
                    ecb.AddComponent(entityInQueryIndex, e, new RockThrownComponent());
                }
            })
            .Schedule(jobHandle);

        jobHandle = Entities
        .WithName("MoveThrownToReset")
        .WithAll<RockComponent, RockThrownComponent>()
        .ForEach(
            (int entityInQueryIndex, Entity e) => 
            {
                float val = rnd.NextFloat(0f, 1f);
                if (val > 0.5f)
                {
                    ecb.RemoveComponent<RockThrownComponent>(entityInQueryIndex, e);
                    ecb.AddComponent(entityInQueryIndex, e, new ResetTag());
                }
            })
            .Schedule(jobHandle);

        jobHandle.Complete();
        return jobHandle;
    }
}