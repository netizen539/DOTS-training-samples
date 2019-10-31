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
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    EntityQuery m_TinCanQuery;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_TinCanQuery = GetEntityQuery(typeof(TinCanComponent), typeof(Translation), typeof(RigidBodyComponent), typeof(ConveyorComponent));
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var tinCansArray = m_TinCanQuery.ToEntityArray(Allocator.TempJob);
        var tinCanPositionsArray = m_TinCanQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var ecb = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        float deltaTime = Time.deltaTime;
        var jobHandle = inputDeps;

        jobHandle = Entities
        .WithName("CheckHitCanSystem")
        .WithAll<RockComponent, InFlightTag>()
        .WithDeallocateOnJobCompletion(tinCansArray)
        .WithDeallocateOnJobCompletion(tinCanPositionsArray)
        .ForEach(
            (int entityInQueryIndex, Entity e, ref Translation pos, ref RigidBodyComponent rigidBody) =>
            {
            int index = 0;
            while (index < tinCansArray.Length)
            {
                Translation tcpos = tinCanPositionsArray[index];
                if (math.distance(pos.Value, tcpos.Value) < 1f)
                {
                    var tce = tinCansArray[index];
                    ecb.AddComponent(entityInQueryIndex, tce, new InFlightTag());

                    Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)index + 1);
                    RigidBodyComponent tinCanRigidBody = new RigidBodyComponent { Gravity = 20f };                    
                    tinCanRigidBody.Velocity = rigidBody.Velocity;
                    tinCanRigidBody.AngularVelocity = math.radians(math.normalize(rand.NextFloat3()) * math.length(rigidBody.Velocity) * 40f);
                    ecb.SetComponent(entityInQueryIndex, tce, tinCanRigidBody);
                    break;
                }
                index++;
            }})
            .Schedule(jobHandle);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        return jobHandle;
    }
}