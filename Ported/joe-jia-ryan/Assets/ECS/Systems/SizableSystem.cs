using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(InitRocksSystem))]
public class SizableSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer().ToConcurrent();
        var deltaTime = Time.deltaTime;

        var jobHandle = Entities
        .WithName("Sizable_System")
        .ForEach(
            (Entity e, ref Scale scale, ref SizeableComponent sizable) => 
            {
                sizable.CurrentSize += (sizable.TargetSize - sizable.CurrentSize) * sizable.ScaleFactor * deltaTime;
                if (sizable.TargetSize - sizable.CurrentSize < 0.01f)
                    sizable.CurrentSize = sizable.TargetSize;
                scale.Value = sizable.CurrentSize;
            })
            .Schedule(inputDeps);

        return jobHandle;
    }
}