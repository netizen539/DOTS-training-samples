using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class ConveyorSystems : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    struct ConveyorJob : IJobForEachWithEntity<Translation, RigidBodyComponent, ConveyorComponent>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public float DeltaTime;

        public void Execute(Entity e, int index, ref Translation translation, ref RigidBodyComponent rigidBody, [ReadOnly] ref ConveyorComponent conveyor)
        {
            rigidBody.Velocity = conveyor.Direction * conveyor.Speed;
            translation.Value += rigidBody.Velocity * DeltaTime;

            if (translation.Value.x * conveyor.Direction.x > conveyor.MaxX)
            {
                CommandBuffer.RemoveComponent<ConveyorComponent>(index, e);
                CommandBuffer.AddComponent(index, e, new ResetTag());
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new ConveyorJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            DeltaTime = Time.DeltaTime
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}