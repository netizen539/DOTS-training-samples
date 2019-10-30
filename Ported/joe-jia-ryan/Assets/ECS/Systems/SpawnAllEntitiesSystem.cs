using System.Collections.Generic;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
public class SpawnAllEntitiesSystem : ComponentSystem
{        

    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref SpawnAllComponent sac, ref LocalToWorld localToWorld) =>                
        {
            int toSpawnCount = sac.Count;

            // Using a .TempJob instead of a .Temp for `spawnPositions`, because the method
            // `RandomPointsInUnitSphere` passes this NativeArray into a Job
            var spawnPositions = new NativeArray<float3>(toSpawnCount, Allocator.TempJob);
            GeneratePoints.RandomPointsInUnitSphere(spawnPositions);

            // Calling Instantiate once per spawned Entity is rather slow, and not recommended
            // This code is placeholder until we add the ability to bulk-instantiate many entities from an ECB
            var entities = new NativeArray<Entity>(toSpawnCount, Allocator.Temp);
            for (int i = 0; i < toSpawnCount; ++i)
            {
                entities[i] = PostUpdateCommands.Instantiate(sac.Prefab);
            }

            for (int i = 0; i < toSpawnCount; i++)
            {
                PostUpdateCommands.SetComponent(entities[i], new LocalToWorld
                {
                    Value = float4x4.TRS(
                        localToWorld.Position + (spawnPositions[i] * 10f),
                        quaternion.LookRotationSafe(spawnPositions[i], math.up()),
                        new float3(1.0f, 1.0f, 1.0f))
                });
            }

            PostUpdateCommands.RemoveComponent<SpawnAllComponent>(e);

            spawnPositions.Dispose();
            entities.Dispose();
        });
    }
}
