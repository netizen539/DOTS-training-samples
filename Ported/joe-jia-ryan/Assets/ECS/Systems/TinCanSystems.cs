using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
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

    struct TinCanSpawnJob : IJobForEachWithEntity<TinCanSpawnComponent, LocalToWorld>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;        
        
        public void Execute(Entity entity, int index, [ReadOnly] ref TinCanSpawnComponent spawnComponent, ref LocalToWorld location)
        {
            for (int i = 0; i < spawnComponent.TotalCount; i++)
            {
                var instance = CommandBuffer.Instantiate(index, spawnComponent.TinCanPrefab);
                CommandBuffer.SetComponent(index, instance, new LocalToWorld { Value = location.Value });
                CommandBuffer.AddComponent(index, instance, new TinCanComponent { RangeY = new Vector2(3f, 8f),
                                                                                  Gravity = spawnComponent.Gravity,
                                                                                  Velocity = Vector3.zero,
                                                                                  AngularVelocity = Vector3.zero });
                CommandBuffer.AddComponent<ResetTag>(index, instance);
            }

            CommandBuffer.DestroyEntity(index, entity);
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new TinCanSpawnJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}

public class TinCanResetSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    struct TinCanResetJob : IJobForEachWithEntity<Translation, Rotation, TinCanComponent, ResetTag>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public Unity.Mathematics.Random Rand;

        public void Execute(Entity e, int index, ref Translation translation, ref Rotation rotation, [ReadOnly] ref TinCanComponent tinCan, [ReadOnly] ref ResetTag resetTag)
        {            
            var position = new float3(Rand.NextFloat(0f, 100f),     ////// !TODO //////
                                      Rand.NextFloat(tinCan.RangeY.x, tinCan.RangeY.y),
                                      15f);
            translation.Value = position;
            rotation.Value = quaternion.identity;
            CommandBuffer.RemoveComponent<ResetTag>(index, e);
            CommandBuffer.AddComponent(index, e, new ConvoyorComponent { });
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new TinCanResetJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            Rand = new Unity.Mathematics.Random(839204)
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}