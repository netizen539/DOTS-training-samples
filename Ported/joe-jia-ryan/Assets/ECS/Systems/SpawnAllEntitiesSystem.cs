using System.Collections.Generic;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
//[UpdateAfter(typeof(TransformSystemGroup))]
public class SpawnAllEntitiesSystem : ComponentSystem
{        

    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref SpawnAllComponent sac) =>                
        {
            int totalCount = sac.Count * 3;
            float3 armSpawnPos = new float3(0, 0, 0);

            PostUpdateCommands.RemoveComponent<SpawnAllComponent>(e);

            // Calling Instantiate once per spawned Entity is rather slow, and not recommended
            // This code is placeholder until we add the ability to bulk-instantiate many entities from an ECB
            int index = 0;
            var entities = new NativeArray<Entity>(totalCount, Allocator.Temp);
            while (index < totalCount)
            {
                entities[index] = PostUpdateCommands.Instantiate(sac.RockPrefab);
        
                PostUpdateCommands.AddComponent(entities[index], new Scale
                {
                    Value = 0f
                });
                PostUpdateCommands.AddComponent(entities[index], new RockComponent());
                PostUpdateCommands.AddComponent(entities[index], new RigidBodyComponent());
                PostUpdateCommands.AddComponent(entities[index], new InitComponentTag()); //TODO Need to fill in data
                PostUpdateCommands.AddComponent(entities[index], new SizeableComponent()); //TODO Need to fill in data
                index++;

                // Spawn Tin Cans
                entities[index] = PostUpdateCommands.Instantiate(sac.TinCanPrefab);
                PostUpdateCommands.AddComponent(entities[index], new Scale { Value = 1f });
                PostUpdateCommands.AddComponent(entities[index], new TinCanComponent { RangeY = new float2(3f, 8f),
                                                                                       ReserveTime = 3
                });
                PostUpdateCommands.AddComponent(entities[index], new RigidBodyComponent { Gravity = 20f, //spawnComponent.Gravity,
                                                                                          Velocity = float3.zero,
                                                                                          AngularVelocity = float3.zero
                });                
                PostUpdateCommands.AddComponent(entities[index], new SizeableComponent { ScaleFactor = 1f, TargetSize = 0.4f, CurrentSize = 0f }); //TODO Need to fill in data
                PostUpdateCommands.AddComponent(entities[index], new InitComponentTag());
                index++;
                
                // Spawn Arms
                entities[index] = PostUpdateCommands.Instantiate(sac.ArmPrefab);
                PostUpdateCommands.SetComponent(entities[index], new Translation
                {
                    Value = armSpawnPos
                });
                armSpawnPos.x += 2.0f;
                index++;
            }

            entities.Dispose();
        });
    }
}
