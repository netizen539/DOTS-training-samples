using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SpawnAllEntitiesSystem))]
public class InitRocksSystem : JobComponentSystem
{
    EntityQuery m_InitDataQuery;

    protected override void OnCreate()
    {
        m_InitDataQuery = GetEntityQuery(typeof(ThrowingArmsSharedDataComponent), typeof(RockSharedDataComponent));
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var throwingArmsComponentArray = m_InitDataQuery.ToComponentDataArray<ThrowingArmsSharedDataComponent>(Allocator.TempJob);
        var rockComponentArray = m_InitDataQuery.ToComponentDataArray<RockSharedDataComponent>(Allocator.TempJob);
        var ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbs.CreateCommandBuffer().ToConcurrent();
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random();
        rnd.InitState();
        var right = new float3(1.0f, 0f, 0f);

        var jobHandle = Entities
        .WithName("InitRocks")
        .WithAll<RockComponent, InitComponentTag>()
        .WithDeallocateOnJobCompletion(throwingArmsComponentArray)
        .WithDeallocateOnJobCompletion(rockComponentArray)
        .ForEach(
            (int entityInQueryIndex, Entity e, ref Translation pos) => 
            {

                var tac = throwingArmsComponentArray[0];
                var rc = rockComponentArray[0];

                ecb.RemoveComponent<InitComponentTag>(entityInQueryIndex, e);
                ecb.SetComponent(entityInQueryIndex, e, new SizeableComponent
                {
                    TargetSize = rnd.NextFloat(rc.MinRockSize, rc.MaxRockSize),
                    CurrentSize = 0f,
                    ScaleFactor = rc.SizeGrowthFactor,
                });                

                var minConveyorX = tac.ConveyorMinX;
                var maxConveyorX = tac.ConveyorMaxX;
                float spacing = ((float)maxConveyorX - (float)minConveyorX) / (float)math.max((tac.ArmCount - 1), 1);
                float3 basePos = new float3(minConveyorX,0f,1.5f);

                pos.Value = basePos + right * spacing * entityInQueryIndex;
                ecb.AddComponent(entityInQueryIndex, e, new ConveyorComponent
                {
                    Speed = tac.ConveyorSpeed,
                    Direction = right,
                    ResetX = minConveyorX,
                    MaxX = maxConveyorX
                });

                ecb.AddComponent(entityInQueryIndex, e, new RigidBodyComponent
                {
                    Gravity = rc.Gravity,
                    Velocity = new float3(0f,0f,0f),
                    AngularVelocity = new float3(0f,0f,0f),
                });                


            })
            .Schedule(inputDeps);

        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}