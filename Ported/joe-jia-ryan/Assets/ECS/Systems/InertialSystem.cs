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
				rbc.Velocity += Up * -rbc.Gravity * deltaTime;
    			rot.Value = math.mul(quaternion.AxisAngle(rbc.AngularVelocity, math.length(rbc.AngularVelocity) * deltaTime), rot.Value);
            })
            .Schedule(jobHandle);

        return jobHandle;
    }
}