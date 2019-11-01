using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ConveyorSystems))]
public class BucketSystems : JobComponentSystem
{
    public BeginSimulationEntityCommandBufferSystem ecbSystem;
    public static NativeMultiHashMap<int, Entity> RockEntitiesBucketedByIndex;
    public static NativeMultiHashMap<int, Entity> TinCanEntitiesBucketedByIndex;

    EntityQuery m_DataQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        ecbSystem =  World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_DataQuery = GetEntityQuery(typeof(ThrowingArmsSharedDataComponent));
        RockEntitiesBucketedByIndex = new NativeMultiHashMap<int, Entity>(10000, Allocator.Persistent);
        TinCanEntitiesBucketedByIndex = new NativeMultiHashMap<int, Entity>(10000, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        RockEntitiesBucketedByIndex.Dispose();
        TinCanEntitiesBucketedByIndex.Dispose();
    }

    [BurstCompile]
    public struct BucketRocksJob : IJobForEachWithEntity<Translation, ConveyorComponent, RockComponent>
    {
        public ThrowingArmsSharedDataComponent throwingArmsComponentArray;
    
        public NativeMultiHashMap<int, Entity>.ParallelWriter EntitiesBucketedByIndex;

        public void Execute(Entity e, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref ConveyorComponent _conveyor, [ReadOnly] ref RockComponent _r)
        {
            var tac = throwingArmsComponentArray;   
            int bucketIndex = (int)((translation.Value.x / (float)tac.ConveyorWidth) * (float)tac.ArmCount);
            EntitiesBucketedByIndex.Add(bucketIndex, e);
        }
    }

    [BurstCompile]
    public struct BucketTinCansJob : IJobForEachWithEntity<Translation, ConveyorComponent, TinCanComponent>
    {
        public ThrowingArmsSharedDataComponent throwingArmsComponentArray;
    
        public NativeMultiHashMap<int, Entity>.ParallelWriter EntitiesBucketedByIndex;

        public void Execute(Entity e, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref ConveyorComponent _conveyor, [ReadOnly] ref TinCanComponent _tc)
        {
            var tac = throwingArmsComponentArray;   
            int bucketIndex = (int)((translation.Value.x / (float)tac.ConveyorWidth) * (float)tac.ArmCount);
            EntitiesBucketedByIndex.Add(bucketIndex, e);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var tacs = m_DataQuery.ToComponentDataArray<ThrowingArmsSharedDataComponent>(Allocator.TempJob);
        var job = new BucketRocksJob
        {
            throwingArmsComponentArray = tacs[0],
            EntitiesBucketedByIndex = BucketSystems.RockEntitiesBucketedByIndex.AsParallelWriter(), 
        }.Schedule(this, inputDeps);
        
        job = new BucketTinCansJob
        {
            throwingArmsComponentArray = tacs[0],
            EntitiesBucketedByIndex = BucketSystems.TinCanEntitiesBucketedByIndex.AsParallelWriter(), 
        }.Schedule(this, job);
        ecbSystem.AddJobHandleForProducer(job);
        tacs.Dispose();
        return job;
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(IdleArmSystem))]
[UpdateAfter(typeof(HoldingArmSystem))]
[UpdateAfter(typeof(CheckHitCanSystem))]
public class UnbucketSystems : JobComponentSystem
{
    public BeginSimulationEntityCommandBufferSystem ecbSystem;
    struct CleanupJob : IJob
    {
        public NativeMultiHashMap<int, Entity> DataForCleanup;
        
        public void Execute()
        {
            DataForCleanup.Clear();
        }
    }

    JobHandle DependencyJobs;

    public void AddJobHandle(JobHandle handle)
    {
        DependencyJobs = JobHandle.CombineDependencies(handle, DependencyJobs);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        ecbSystem =  World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new CleanupJob{
            DataForCleanup = BucketSystems.RockEntitiesBucketedByIndex,
        }.Schedule(JobHandle.CombineDependencies(DependencyJobs, inputDeps));
        DependencyJobs = default;
        
        job = new CleanupJob{
            DataForCleanup = BucketSystems.TinCanEntitiesBucketedByIndex,
        }.Schedule(job);

        ecbSystem.AddJobHandleForProducer(job);
        return job;
    }
}
