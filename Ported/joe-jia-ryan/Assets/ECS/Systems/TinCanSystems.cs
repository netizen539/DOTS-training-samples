using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class TinCanSpawnSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    struct TinCanSpawnJob : IJobForEachWithEntity<TinCanSpawnComponent, LocalToWorld>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;        
        
        public void Execute(Entity entity, int index, [ReadOnly] ref TinCanSpawnComponent spawnComponent, [ReadOnly] ref LocalToWorld location)
        {
            for (int i = 0; i < spawnComponent.TotalCount; i++)
            {

            }
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new TinCanSpawnJob();
        return job.Schedule(this, inputDeps);
    }
}