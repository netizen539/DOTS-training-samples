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
        Entities.ForEach((Entity e, ref SpawnAllComponent sac) =>                
        {
            int totalCount = sac.Count * 2;

            PostUpdateCommands.RemoveComponent<SpawnAllComponent>(e);

            // Using a .TempJob instead of a .Temp for `spawnPositions`, because the method
            // `RandomPointsInUnitSphere` passes this NativeArray into a Job
            var spawnPositions = new NativeArray<float3>(totalCount, Allocator.TempJob);
            GeneratePoints.RandomPointsInUnitSphere(spawnPositions);

            // Calling Instantiate once per spawned Entity is rather slow, and not recommended
            // This code is placeholder until we add the ability to bulk-instantiate many entities from an ECB
            int index = 0;
            var entities = new NativeArray<Entity>(totalCount, Allocator.Temp);
            while (index < totalCount)
            {
                entities[index] = PostUpdateCommands.Instantiate(sac.RockPrefab);
                PostUpdateCommands.SetComponent(entities[index], new Translation
                {
                    Value = spawnPositions[index] * 10f
                });
                PostUpdateCommands.AddComponent(entities[index], new Scale
                {
                    Value = 0f
                });
                PostUpdateCommands.AddComponent(entities[index], new RockComponent());
                PostUpdateCommands.AddComponent(entities[index], new InitComponentTag()); //TODO Need to fill in data
                PostUpdateCommands.AddComponent(entities[index], new SizeableComponent()); //TODO Need to fill in data
                index++;

                entities[index] = PostUpdateCommands.Instantiate(sac.TinCanPrefab);
                PostUpdateCommands.SetComponent(entities[index], new Translation
                {
                    Value = spawnPositions[index] * 5f
                });
                PostUpdateCommands.AddComponent(entities[index], new Scale
                {
                    Value = 0f
                });
                PostUpdateCommands.AddComponent(entities[index], new TinCanComponent());
                PostUpdateCommands.AddComponent(entities[index], new InitComponentTag()); //TODO Need to fill in data
                PostUpdateCommands.AddComponent(entities[index], new SizeableComponent()); //TODO Need to fill in data
                index++;
            }

            spawnPositions.Dispose();
            entities.Dispose();
        });
    }
}
