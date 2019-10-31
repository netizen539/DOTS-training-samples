using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CheckAndResetSystem))]
public class TinCanInitSystem : JobComponentSystem

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
    struct TinCanInitJob : IJobForEachWithEntity<Translation, Scale, TinCanComponent, RigidBodyComponent, InitComponentTag>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public Unity.Mathematics.Random Rand;
        public float TotalArmsWidth;

        public void Execute(Entity e, int index, ref Translation translation, ref Scale scale, [ReadOnly] ref TinCanComponent tinCan, ref RigidBodyComponent rigidBody, [ReadOnly] ref InitComponentTag initTag)
        {
            var position = new float3(Rand.NextFloat(0f, TotalArmsWidth + 10f),
                                      Rand.NextFloat(tinCan.RangeY.x, tinCan.RangeY.y),
                                      15f);
            translation.Value = position;

            scale.Value = 0f;
            rigidBody.Velocity = rigidBody.AngularVelocity = float3.zero;

            CommandBuffer.RemoveComponent<InitComponentTag>(index, e);
            CommandBuffer.AddComponent(index, e, new ConveyorComponent { Direction = Vector3.left,
                                                                         MaxX = 0f,
                                                                         ResetX = TotalArmsWidth + 10f,
                                                                         Speed = 3f
            });            
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var throwingArmsComponentArray = m_ArmDataQuery.ToComponentDataArray<ThrowingArmsSharedDataComponent>(Allocator.TempJob);

        var job = new TinCanInitJob {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            Rand = new Unity.Mathematics.Random(43255),
            TotalArmsWidth = throwingArmsComponentArray[0].ConveyorWidth
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        throwingArmsComponentArray.Dispose();

        return job;
    }
}

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

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        var throwingArmsComponentArray = m_ArmDataQuery.ToComponentDataArray<ThrowingArmsSharedDataComponent>(Allocator.TempJob);
        float TotalArmsWidth = throwingArmsComponentArray[0].ConveyorWidth;

        var job = Entities.WithAll<TinCanComponent, ResetTag>().ForEach(
            (int entityInQueryIndex, Entity e, ref Translation translation, ref Rotation rotation, ref Scale scale, ref SizeableComponent sizeable, ref RigidBodyComponent rigidBody) =>
            {
                commandBuffer.RemoveComponent<ReservedTag>(entityInQueryIndex, e);
                commandBuffer.RemoveComponent<ResetTag>(entityInQueryIndex, e);

                translation.Value.x = TotalArmsWidth + 10f;
                rotation.Value = quaternion.identity;
                scale.Value = 0.001f;
                sizeable.CurrentSize = 0.001f;
                rigidBody.Velocity = float3.zero;
                rigidBody.AngularVelocity = float3.zero;                

                commandBuffer.AddComponent(entityInQueryIndex, e, new ConveyorComponent
                {
                    Direction = Vector3.left,
                    MaxX = 0f,
                    Speed = 3f
                });
            }).Schedule(inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        throwingArmsComponentArray.Dispose();

        return job;
    }
}

public class TinCanReservedSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    private float Total;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    struct TinCanUnreserveJob : IJobForEachWithEntity<TinCanComponent, ConveyorComponent, ReservedTag>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public float TotalTime;

        public void Execute(Entity e, int index, [ReadOnly] ref TinCanComponent tinCan, [ReadOnly] ref ConveyorComponent conveyor, [ReadOnly] ref ReservedTag reserved)
        {
            if (TotalTime > tinCan.ReserveTime)
                CommandBuffer.RemoveComponent<ReservedTag>(index, e);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Total += Time.DeltaTime;
        var job = new TinCanUnreserveJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            TotalTime = Total
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}

   

//public class TinCanInFlightSystem : JobComponentSystem
//{
//    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
//    }

//    [BurstCompile]
//    struct TinCanInFlightJob : IJobForEachWithEntity<Translation, Rotation, TinCanComponent, RigidBodyComponent, InFlightTag>
//    {
//        public EntityCommandBuffer.Concurrent CommandBuffer;
//        public float DeltaTime;

//        public void Execute(Entity e, int index, ref Translation translation, ref Rotation rotation, [ReadOnly] ref TinCanComponent tinCan, ref RigidBodyComponent rigidBody, [ReadOnly] ref InFlightTag inFlight)
//        {
//            CommandBuffer.RemoveComponent<ConveyorComponent>(index, e);

//            translation.Value += rigidBody.Velocity * DeltaTime;
//            rigidBody.Velocity += new float3(0f, rigidBody.Gravity * -1f * DeltaTime, 0f);
//            rotation.Value = quaternion.AxisAngle(rigidBody.AngularVelocity, math.length(rigidBody.AngularVelocity) * DeltaTime);
//        }
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        var job = new TinCanInFlightJob
//        {
//            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
//            DeltaTime = Time.DeltaTime
//        }.Schedule(this, inputDeps);

//        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

//        return job;
//    }
//}

/*[UpdateInGroup(typeof(SimulationSystemGroup))]
public class TinCanSpawnSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

=======
/*[UpdateInGroup(typeof(SimulationSystemGroup))]
public class TinCanSpawnSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

>>>>>>> master
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
