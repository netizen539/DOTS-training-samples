using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(RockThrownSystem))]
public class InertialSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float3 Up = new float3(0f,1f,0f);

        var ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = ecbs.CreateCommandBuffer().ToConcurrent();
        float deltaTime = UnityEngine.Time.deltaTime;
        var jobHandle = inputDeps;

        jobHandle = Entities
        .WithName("InertialSystem")
        .WithAll<InFlightTag>()
        .ForEach(
            (int entityInQueryIndex, Entity e, ref RigidBodyComponent rbc, ref Translation pos, ref Rotation rot) => 
            {
                pos.Value += rbc.Velocity * deltaTime;
				rbc.Velocity -= Up * rbc.Gravity * deltaTime;
                quaternion rotation = math.mul(quaternion.AxisAngle(rbc.AngularVelocity, math.length(rbc.AngularVelocity) * deltaTime), rot.Value);
                rotation = rotation.value % (math.PI * 2f);
                rot.Value = rotation;
                //         if (math.isnan(rot.Value.value.x))
                //         {
                //             Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)entityInQueryIndex + 1);
                //             rbc.AngularVelocity = math.radians(math.normalize(rand.NextFloat3()) * math.length(rbc.Velocity) * 40f);
                //         }

            })
            .Schedule(jobHandle);

        ecbs.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
} 