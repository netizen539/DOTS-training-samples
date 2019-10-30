using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;


public class IdleArmSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle handle = Entities
            .ForEach( (Entity entity, ref ArmIdleTag tag, ref ArmComponent armComponent) =>
            {
            }).Schedule(inputDeps);
        return handle;
    }
}

public class ReachingArmSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle handle = Entities
            .ForEach( (Entity entity, ref ArmReachingTag tag, ref ArmComponent armComponent) =>
        {
        }).Schedule(inputDeps);
        return handle;
    }
}

public class HoldingArmSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle handle = Entities
            .ForEach( (Entity entity, ref ArmHoldingTag tag, ref ArmComponent armComponent) =>
            {
            }).Schedule(inputDeps);
        return handle;
    }
}

public class ThrowingArmSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle handle = Entities
            .ForEach( (Entity entity, ref ArmThrowingTag tag, ref ArmComponent armComponent) =>
            {
            }).Schedule(inputDeps);
        return handle;
    }
}

public class ResetArmSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle handle = Entities
            .ForEach( (Entity entity, ref ArmResetTag tag, ref ArmComponent armComponent) =>
            {
            }).Schedule(inputDeps);
        return handle;
    }
}
