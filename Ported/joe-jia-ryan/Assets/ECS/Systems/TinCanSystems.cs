using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CheckAndResetSystem))]
public class TinCanResetSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    EntityQuery m_ArmDataQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_ArmDataQuery = GetEntityQuery(typeof(ThrowingArmsSharedDataComponent));
    }

    [BurstCompile]
    struct TinCanInitJob : IJobForEachWithEntity<Translation, Rotation, Scale, TinCanComponent, RigidBodyComponent, ResetTag>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public Unity.Mathematics.Random Rand;
        public float TotalArmsWidth;

        public void Execute(Entity e, int index, ref Translation translation, ref Rotation rotation, ref Scale scale, [ReadOnly] ref TinCanComponent tinCan, ref RigidBodyComponent rigidBody, [ReadOnly] ref ResetTag resetTag)
        {
            var position = new float3(Rand.NextFloat(0f, TotalArmsWidth + 10f),
                                      Rand.NextFloat(tinCan.RangeY.x, tinCan.RangeY.y),
                                      15f);
            translation.Value = position;
            rotation.Value = quaternion.identity;
            scale.Value = 0f;
            rigidBody.Velocity = rigidBody.AngularVelocity = float3.zero;

            CommandBuffer.RemoveComponent<ReservedTag>(index, e);
            CommandBuffer.RemoveComponent<ResetTag>(index, e);

            CommandBuffer.AddComponent(index, e, new ConveyorComponent { Direction = Vector3.left,
                                                                         MaxX = 0f,
                                                                         ResetX = 100f,
                                                                         Speed = 3f
            });
            
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var throwingArmsComponentArray = m_ArmDataQuery.ToComponentDataArray<ThrowingArmsSharedDataComponent>(Allocator.TempJob);

        var job = new TinCanInitJob {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            Rand = new Unity.Mathematics.Random( (uint)(System.DateTime.Now.Millisecond+1)),
            TotalArmsWidth = throwingArmsComponentArray[0].ConveyorWidth
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        throwingArmsComponentArray.Dispose();

        return job;
    }
}

public class TinCanSizeableSystem : JobComponentSystem
{
    [BurstCompile]
    struct TinCanSizeableJob : IJobForEach<Scale, TinCanComponent, SizeableComponent>
    {
        public float DeltaTime;

        public void Execute(ref Scale scale, [ReadOnly] ref TinCanComponent tinCan, [ReadOnly] ref SizeableComponent sizeable)
        {
            scale.Value += scale.Value >= 1f ? 0f : (1f - scale.Value) * sizeable.ScaleFactor * DeltaTime;          
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new TinCanSizeableJob { DeltaTime = Time.DeltaTime };
        return job.Schedule(this, inputDeps);
    }
}


/*[UpdateInGroup(typeof(SimulationSystemGroup))]
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
}*/
