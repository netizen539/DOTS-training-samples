using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(InertialSystem))]
public class CheckHitCanSystem : JobComponentSystem
{
    EntityQuery m_TinCanQuery;

    protected override void OnCreate()
    {
        m_TinCanQuery = GetEntityQuery(typeof(TinCanComponent), typeof(Translation));
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var tinCansArray = m_TinCanQuery.ToEntityArray(Allocator.TempJob);
        var tinCanPositionsArray = m_TinCanQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer().ToConcurrent();
        float deltaTime = Time.deltaTime;
        var jobHandle = inputDeps;

        jobHandle = Entities
        .WithName("CheckHitCanSystem")
        .WithAll<RockComponent, InFlightTag>()
        .WithDeallocateOnJobCompletion(tinCansArray)
        .WithDeallocateOnJobCompletion(tinCanPositionsArray)
        .ForEach(
            (int entityInQueryIndex, Entity e, ref Translation pos) => 
            {
                int index = 0;
                while (index < tinCansArray.Length)
                {
                    Translation tcpos = tinCanPositionsArray[index];
                    if (math.distance(pos.Value, tcpos.Value) < 0.25f)
                    {
                        var tce = tinCansArray[index];
                        ecb.AddComponent(entityInQueryIndex, tce, new TinCanHitTag());
                        break;
                    }
                    index++;
                }
            })
            .Schedule(jobHandle);

 
        return jobHandle;
    }
}