using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

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
        var ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer().ToConcurrent();
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random();
        rnd.InitState();

        var jobHandle = Entities
        .WithName("Init_Rocks")
        .WithAll<RockComponent, InitComponentTag>()
        .ForEach(
            (int entityInQueryIndex, Entity e, ref Translation pos) => 
            {

                var tac = throwingArmsComponentArray[0];
                var rc = rockComponentArray[0];

                ecb.RemoveComponent<InitComponentTag>(entityInQueryIndex, e);
                ecb.AddComponent(entityInQueryIndex, e, new ConveyorComponent
                {
                    Speed = tac.ConveyorSpeed,
                    Direction = Vector3.right,
                    ResetX = tac.ConveyorMinX + tac.ConveyorMargin,
                    MaxX = tac.ConveyorMaxX
                });
                ecb.SetComponent(entityInQueryIndex, e, new SizeableComponent
                {
                    TargetSize = rnd.NextFloat(rc.MinRockSize, rc.MaxRockSize),
                    CurrentSize = 0f,
                    ScaleFactor = rc.SizeGrowthFactor,
                });                
            })
            .Schedule(inputDeps);

        jobHandle.Complete();
        rockComponentArray.Dispose();
        throwingArmsComponentArray.Dispose();
        return jobHandle;
    }
}