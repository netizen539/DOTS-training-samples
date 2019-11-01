using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

public class directions
{
    [ReadOnly]
    public static float3 right = new float3(1,0,0);
    [ReadOnly]
    public static float3 up = new float3(0,1,0);
    [ReadOnly]
    public static float3 one = new float3(1,1,1);
    [ReadOnly]
    public static float3 forward = new float3(0, 0, 1);
}

public class util
{
    public static float3 Last(float3[] array, int indexFromEnd)
    {
        return array[array.Length - indexFromEnd - 1];
    }
    
    public static void UpdateMatrices(Matrix4x4[] matrices, float3[] chain, int index, float thickness, float3 up) {
        // find the rendering matrices for an IK chain
        // (each pair of neighboring points is connected by a beam)
        for (int i=0;i<chain.Length-1;i++) {
            float3 delta = chain[i + 1] - chain[i];
            matrices[index + i] = Matrix4x4.TRS(chain[i] + delta * .5f,Quaternion.LookRotation(delta,up),new Vector3(thickness,thickness,math.length(delta)));
        }
    }
    
    private static GlobalData globalData = null;
    public static GlobalData GetGlobalData()
    {
        if (globalData)
            return globalData;
        
        GameObject globalDataGO = GameObject.Find("/GlobalData");
        if (!globalDataGO)
        {
            Debug.LogError("You need to have a GlobalData gameobject for the thrower arms.");
            return null;
        }

        globalData = globalDataGO.GetComponent<GlobalData>();
        return globalData;
    }
    
    public static float3 AimAtCan(float3 tinCanVelocity, float3 tinCanPosition, float3 startPos, float gravity) {

        // predictive aiming based on this article by Kain Shin:
        // https://www.gamasutra.com/blogs/KainShin/20090515/83954/Predictive_Aim_Mathematics_for_AI_Targeting.php

        float targetSpeed = math.length(tinCanVelocity);
        float cosTheta = math.dot(math.normalize(startPos - tinCanPosition), math.normalize(tinCanVelocity));
        float D = math.length(tinCanPosition - startPos);

        float baseThrowSpeed = 24f;
        // quadratic equation terms
        float A = baseThrowSpeed * baseThrowSpeed - targetSpeed * targetSpeed;
        float B = (2f * D * targetSpeed * cosTheta);
        float C = -D * D;

        if (B * B < 4f * A * C) {
            // it's impossible to hit the target
            return directions.forward*10f+directions.up*8f;
        }

        // quadratic equation has two possible outputs
        float t1 = (-B + math.sqrt(B*B - 4f * A * C))/(2f*A);
        float t2 = (-B - math.sqrt(B*B - 4f * A * C))/(2f*A);

        // our two t values represent two possible trajectory durations.
        // pick the best one - whichever is lower, as long as it's positive
        float t;
        if (t1 < 0f && t2 < 0f) {
            // both potential collisions take place in the past!
            return directions.forward * 10f + directions.up * 8f;
        } else if (t1<0f && t2>0f) {
            t = t2;
        } else if (t1>0f && t2<0f) {
            t = t1;
        } else {
            t = Mathf.Min(t1,t2);
        }

        if (float.IsNaN(t))
        {
            t = 1f;
        }

        if (float.IsNaN(cosTheta))
        {
            cosTheta = 0f;
        }
        
        float3 output = tinCanVelocity -.5f*new float3(0f,-gravity,0f)*t + (tinCanPosition - startPos) / t;

        if (math.length(output) >baseThrowSpeed*2f) {
            // the required throw is too serious for us to handle
            return directions.forward * 10f + directions.up * 8f;
        }

        return output;
    }
    
}

[AlwaysUpdateSystem]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BucketSystems))]
public class IdleArmSystem : JobComponentSystem
{
    public ComponentDataFromEntity<Translation> translationTypeFromEntity;
    public EntityQueryDesc queryDescription;
    public EntityQuery query;
    public BeginSimulationEntityCommandBufferSystem ecbSystem;
    
    [BurstCompile]
    struct IdleArmJob : IJobForEachWithEntity<ArmIdleTag, ArmComponent, Translation>
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> translationsFromEntity;
        [ReadOnly] public NativeMultiHashMap<int, Entity> EntitiesBucketedByIndex;
        public EntityCommandBuffer.Concurrent ecb;

        public float worldTime;

        public void Execute(Entity armEntity, int index, [ReadOnly] ref ArmIdleTag tag, [ReadOnly] ref ArmComponent armComponent, [ReadOnly] ref Translation translation)
        {
            float3 searchFromPos = translation.Value - directions.right * .5f;
            float tmpDistance = float.MaxValue;
            float reachMaxSquared = armComponent.maxReachLength * armComponent.maxReachLength; 
            Entity closestRock = Entity.Null;

            float time = worldTime + armComponent.timeOffset;
            armComponent.idleHandTarget = translation.Value+new float3(Mathf.Sin(time)*.35f,1f+Mathf.Cos(time*1.618f)*.5f,1.5f);
            armComponent.handTarget = armComponent.idleHandTarget;

            var rockEntities = EntitiesBucketedByIndex.GetValuesForKey(index);
            foreach (Entity re in rockEntities)
            {
                Translation targetRockPos = translationsFromEntity[re];
                float distSq = math.distancesq(searchFromPos, targetRockPos.Value);
                if ((distSq < reachMaxSquared) && (distSq < tmpDistance))
                {
                    tmpDistance = distSq;
                    closestRock = re;
                }
            }

            if (closestRock != Entity.Null)
            {
                ecb.AddComponent(index, closestRock, new ReservedTag());
                ecb.RemoveComponent<ArmIdleTag>(index, armEntity);
                ecb.AddComponent(index, armEntity, new ArmReachingTag
                {
                    rockToReachFor = closestRock
                });
            }
        }
    }

    protected override void OnCreate()
    {
        var queryDescription = new EntityQueryDesc
        {
            None = new ComponentType[] {ComponentType.ReadOnly<ReservedTag>()},
            All = new ComponentType[] { ComponentType.ReadOnly<RockComponent>() , ComponentType.ReadOnly<Translation>(), }
        };
        query = GetEntityQuery(queryDescription);
        ecbSystem =  World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        translationTypeFromEntity = GetComponentDataFromEntity<Translation>(true);


          var handle = new IdleArmJob()
          {
                translationsFromEntity = translationTypeFromEntity,
                EntitiesBucketedByIndex = BucketSystems.EntitiesBucketedByIndex,
                ecb = ecbSystem.CreateCommandBuffer().ToConcurrent(),
                worldTime = Time.time
          }
          .Schedule(this, inputDeps);
        World.GetOrCreateSystem<UnbucketSystems>().AddJobHandle(handle);
        ecbSystem.AddJobHandleForProducer(handle);

      return handle;

    }
}

[AlwaysUpdateSystem]
public class ReachingArmSystem : JobComponentSystem
{
    public ComponentDataFromEntity<Translation> translationTypeFromEntity;
    public ComponentDataFromEntity<Scale> scaleTypeFromEntity;

    public BeginInitializationEntityCommandBufferSystem ecbSystem;

    //[BurstCompile]
    struct ReachingArmJob : IJobForEachWithEntity<ArmReachingTag, ArmComponent, Translation>
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> translationsFromEntity;
        [ReadOnly] public ComponentDataFromEntity<Scale> scalesFromEntity;
        public EntityCommandBuffer.Concurrent ecb;
        [ReadOnly] public float deltaTime;
        
        public void Execute(Entity armEntity, int index, [ReadOnly] ref ArmReachingTag tag, ref ArmComponent armComponent, [ReadOnly] ref Translation translation)
        {
            Entity intendedRock = tag.rockToReachFor;
            Translation intendedRockTranslation = translationsFromEntity[intendedRock];
            Scale intendedRockScale = scalesFromEntity[intendedRock];
            float3 delta = intendedRockTranslation.Value - translation.Value;

            if (math.lengthsq(delta) < armComponent.maxReachLength * armComponent.maxReachLength)
            {
                // Rock is still within reach, attempt to reach it.
                float3 flatDelta = delta;
                flatDelta.y = 0;
                flatDelta = math.normalize(flatDelta);

                float intendedRockSize = intendedRockScale.Value;
                armComponent.lastIntendedRockSize = intendedRockSize;
                armComponent.grabHandTarget = intendedRockTranslation.Value + directions.up * intendedRockSize * .5f -
                                         flatDelta * intendedRockSize * .5f;
                armComponent.lastIntendedRockPos = intendedRockTranslation.Value;
                
                armComponent.reachingTimer += deltaTime / armComponent.reachDuration;
                if (armComponent.reachingTimer >= 1f)
                {
                    // We've arrived at the rock
                    ecb.RemoveComponent<ConveyorComponent>(index, intendedRock);
                    ecb.AddComponent(index, intendedRock, new RockHeldComponent());
                    ecb.RemoveComponent<ArmReachingTag>(index, armEntity);
                    ecb.AddComponent(index, armEntity, new ArmHoldingTag());
                    
                    armComponent.windupTimer = -.5f;
                    armComponent.throwTimer = 0f;
                    armComponent.heldRock = intendedRock;
                    armComponent.heldRockOffset = armComponent.handMatrix.inverse.MultiplyPoint3x4(intendedRockTranslation.Value);
                }
                else
                {
                    // We're still reaching for the rock
                    // smoothed reach timer
                    float grabT = armComponent.reachingTimer;
                    grabT = 3f * grabT * grabT - 2f * grabT * grabT * grabT;
                    armComponent.savedGrabT = grabT;

                    armComponent.reachingTimer = Mathf.Clamp01(armComponent.reachingTimer);

                    // reaching overrides our idle hand position
                    armComponent.handTarget = math.lerp (armComponent.idleHandTarget, armComponent.grabHandTarget, armComponent.savedGrabT);
                }
            }
            else
            {
                // Rock has eluded our grasp. Unreserve the rock.
                ecb.RemoveComponent<ReservedTag>(index, intendedRock);
                ecb.RemoveComponent<ArmReachingTag>(index, armEntity);
                ecb.AddComponent(index, armEntity, new ArmResetTag());
            }
        }
    }

    protected override void OnCreate()
    {
        ecbSystem =  World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        translationTypeFromEntity = GetComponentDataFromEntity<Translation>(true);
        scaleTypeFromEntity = GetComponentDataFromEntity<Scale>(true);

        var reachingJob = new ReachingArmJob()
        {
            translationsFromEntity = translationTypeFromEntity,
            scalesFromEntity = scaleTypeFromEntity,
            ecb = ecbSystem.CreateCommandBuffer().ToConcurrent(),
            deltaTime = Time.deltaTime
        };
        var handle = reachingJob.Schedule(this, inputDeps);
        ecbSystem.AddJobHandleForProducer(handle);
        return handle;
    }
}

[AlwaysUpdateSystem]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BucketSystems))]
public class HoldingArmSystem : JobComponentSystem
{
    public BeginInitializationEntityCommandBufferSystem ecbSystem;
    public ComponentDataFromEntity<RockHeldComponent> rockHeldCompTypeFromEntity;
    public EntityQueryDesc queryDescription;
    public EntityQuery query;
    
    struct HoldingArmJob : IJobForEachWithEntity<ArmHoldingTag, ArmComponent, Translation>
    {
        [ReadOnly] public ComponentDataFromEntity<RockHeldComponent> rockHeldCompsFromEntity;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
        [ReadOnly] public ArchetypeChunkComponentType<Translation> translationType;
        [ReadOnly] public ArchetypeChunkEntityType entityType;
        public float deltaTime;
        public EntityCommandBuffer.Concurrent ecb;
        
        public void Execute(Entity entity, int index, [ReadOnly] ref ArmHoldingTag tag, ref ArmComponent armComponent, [ReadOnly] ref Translation translation)
        {
            float tmpDistance = float.MaxValue;
            float range = 50.0f;
            float maxThrowRange = range * range;
            Entity closestCan = Entity.Null;

            armComponent.reachingTimer -= deltaTime / armComponent.reachDuration;

            if (armComponent.targetCan == Entity.Null)
            {
                // Find nearest target can to fire at.
                for (int i = 0; i < chunks.Length; i++)
                {
                    var canTranslations = chunks[i].GetNativeArray(translationType);
                    var canEntities = chunks[i].GetNativeArray(entityType);
                
                    for (int j = 0; j < canTranslations.Length; j++)
                    {
                        Translation canTrans = canTranslations[j];
                        Entity rockEntitiy = canEntities[j];
                    
                        float distSq = math.distancesq(translation.Value, canTrans.Value);
                        if ((distSq < maxThrowRange) && (distSq < tmpDistance))
                        {
                            tmpDistance = distSq;
                            closestCan = rockEntitiy;
                        }
                    }
                }
                
                if (closestCan != Entity.Null)
                {
                    armComponent.targetCan = closestCan;
                }
            }
        
            if (armComponent.targetCan != Entity.Null)
            {
                ecb.AddComponent(index, armComponent.targetCan, new ReservedTag());
                ecb.RemoveComponent<ArmHoldingTag>(index, entity);
                ecb.AddComponent(index, entity, new ArmThrowingTag());
            }

            if (armComponent.heldRock != Entity.Null)
            {
                //Hand matrix is set in the animation system, here we tell the rock where we want it to reposition.
                RockHeldComponent heldComponent = rockHeldCompsFromEntity[armComponent.heldRock];
                heldComponent.rockInHandPosition =
                    armComponent.handMatrix.MultiplyPoint3x4(armComponent.heldRockOffset);
            }

        }
    }

    protected override void OnCreate()
    {
        var queryDescription = new EntityQueryDesc
        {
            None = new ComponentType[] {ComponentType.ReadOnly<ReservedTag>()},
            All = new ComponentType[] { ComponentType.ReadOnly<TinCanComponent>() , ComponentType.ReadOnly<Translation>()}
        };
        query = GetEntityQuery(queryDescription);
        ecbSystem =  World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        rockHeldCompTypeFromEntity = GetComponentDataFromEntity<RockHeldComponent>(true);

        var job = new HoldingArmJob()
        {
            rockHeldCompsFromEntity = rockHeldCompTypeFromEntity,
            deltaTime = Time.deltaTime,
            ecb = ecbSystem.CreateCommandBuffer().ToConcurrent(),
            chunks = query.CreateArchetypeChunkArray(Allocator.TempJob),
            translationType = GetArchetypeChunkComponentType<Translation>(),
//            rigidBodyType = GetArchetypeChunkComponentType<RigidBodyComponent>(),
            entityType = GetArchetypeChunkEntityType(),
        };
        var handle = job.Schedule(this, inputDeps);
        ecbSystem.AddJobHandleForProducer(handle);
        return handle;
    }
}

[AlwaysUpdateSystem]
public class ThrowingArmSystem : JobComponentSystem
{
    public ComponentDataFromEntity<Translation> translationTypeFromEntity;
    public ComponentDataFromEntity<RigidBodyComponent> rigidBodyTypeFromEntity;
    public BeginSimulationEntityCommandBufferSystem ecbSystem;

    struct ThrowingArmJob : IJobForEachWithEntity<ArmThrowingTag, ArmComponent, Translation>
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> translationsFromEntity;
        [ReadOnly] public ComponentDataFromEntity<RigidBodyComponent> rigidBodyFromEntity;
        public float deltaTime;
        public EntityCommandBuffer.Concurrent ecb;

        public void Execute(Entity entity, int index, [ReadOnly] ref ArmThrowingTag tag, ref ArmComponent armComponent, [ReadOnly] ref Translation translation)
        {
            armComponent.windupTimer += deltaTime / armComponent.windupDuration;
            RigidBodyComponent targetCanRigidBody = rigidBodyFromEntity[armComponent.targetCan];
            Translation targetCanTranslation = translationsFromEntity[armComponent.targetCan];

            if (armComponent.windupTimer < 1f)
            {

                // still winding up...
                float windupT = Mathf.Clamp01(armComponent.windupTimer) - Mathf.Clamp01(armComponent.throwTimer * 2f);
                windupT = 3f * windupT * windupT - 2f * windupT * windupT * windupT;
                armComponent.handTarget = Vector3.Lerp(armComponent.handTarget,armComponent.windupHandTarget,windupT);
                float3 flatTargetDelta = targetCanTranslation.Value - translation.Value;
                flatTargetDelta.y = 0f;
                flatTargetDelta = math.normalize(flatTargetDelta);

                // windup position is "behind us," relative to the target position
                armComponent.windupHandTarget = translation.Value - flatTargetDelta * 2f + directions.up * (3f - windupT * 2.5f);
                
            }
            else
            {
                // done winding up - actual throw, plus resetting to idle
                armComponent.throwTimer += deltaTime / armComponent.throwDuration;

                // update our aim until we release the rock
                //if (armComponent.heldRock != Entity.Null)
                //{
                armComponent.aimVector = util.AimAtCan(targetCanRigidBody.Velocity,targetCanTranslation.Value, translation.Value, targetCanRigidBody.Gravity);
                //}

                // we start this animation in our windup position,
                // and end it by returning to our default idle pose
                float3 restingPos = math.lerp(armComponent.windupHandTarget,armComponent.handTarget, armComponent.throwTimer);

                // find the hand's target position to perform the throw
                // (somewhere forward and upward from the windup position)
                float3 throwHandTarget = armComponent.windupHandTarget + (math.normalize(armComponent.aimVector) * 2.5f);
                float lerpAmount = util.GetGlobalData().armThrowingCurve.Evaluate(armComponent.throwTimer);
                armComponent.handTarget = math.lerp(restingPos,throwHandTarget, lerpAmount);
                
                if (armComponent.throwTimer > .15f && armComponent.heldRock != Entity.Null) {
                    // release the rock
                    ecb.RemoveComponent<RockHeldComponent>(index, armComponent.heldRock);
                    var rockThrownComp = new RockThrownComponent
                    {
                        Velocity = armComponent.aimVector
                    };
                    ecb.AddComponent(index, armComponent.heldRock, rockThrownComp);
                    armComponent.heldRock = Entity.Null;
                }

                if (armComponent.throwTimer >= 1f) {
                    // we've completed the animation - return to idle
                    armComponent.windupTimer = 0f;
                    armComponent.throwTimer = 0f;
                    armComponent.targetCan = Entity.Null;
                    
                    ecb.RemoveComponent<ArmThrowingTag>(index, entity);
                    ecb.AddComponent(index, entity, new ArmResetTag());
                }
            }
        }
    }
    protected override void OnCreate()
    {
        ecbSystem =  World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        translationTypeFromEntity = GetComponentDataFromEntity<Translation>(true);
        rigidBodyTypeFromEntity = GetComponentDataFromEntity<RigidBodyComponent>(true);

        var job = new ThrowingArmJob()
        {
            translationsFromEntity = translationTypeFromEntity,
            rigidBodyFromEntity = rigidBodyTypeFromEntity,
            deltaTime = Time.deltaTime,
            ecb = ecbSystem.CreateCommandBuffer().ToConcurrent()
        };
        var handle = job.Schedule(this, inputDeps);
        World.GetOrCreateSystem<UnbucketSystems>().AddJobHandle(handle);
        ecbSystem.AddJobHandleForProducer(handle);
        return handle;
    }
}

[AlwaysUpdateSystem]
public class ResetArmSystem : JobComponentSystem
{
    public EndSimulationEntityCommandBufferSystem ecbSystem;

    struct ResetArmJob : IJobForEachWithEntity<ArmResetTag, ArmComponent>
    {
        public EntityCommandBuffer.Concurrent ecb;
        
        public void Execute(Entity entity, int index, [ReadOnly] ref ArmResetTag tag, ref ArmComponent armComponent)
        {
            armComponent.grabHandTarget = new float3();
            armComponent.lastIntendedRockSize = 0;
            armComponent.lastIntendedRockPos = new float3();
            armComponent.savedGrabT = 0f;
            armComponent.reachingTimer = 0f;
            armComponent.throwTimer = 0f;
            armComponent.windupTimer = 0f;
            
            ecb.RemoveComponent<ArmResetTag>(index, entity);
            ecb.AddComponent(index, entity, new ArmIdleTag());
        }
    }
    
    protected override void OnCreate()
    {
        ecbSystem =  World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new ResetArmJob()
        {
            ecb = ecbSystem.CreateCommandBuffer().ToConcurrent()
        };
        var handle = job.Schedule(this, inputDeps);
        ecbSystem.AddJobHandleForProducer(handle);
        return handle;
    }
}

// FABRIK: Forward-And-Backward-Reaching Inverse Kinematics
// "Each tick:  Drag the chain (from the end) to the target point,
//  then drag the chain (from the root) back to the anchor point"
public static class FABRIK {
	
    public static void Solve(float3[] chain, float boneLength, float3 anchor, float3 target, float3 bendHint) {
        chain[chain.Length - 1] = target;
        for (int i=chain.Length-2;i>=0;i--) {
            chain[i] += bendHint;
            float3 delta = chain[i] - chain[i + 1];
            chain[i] = chain[i + 1] + math.normalize(delta) * boneLength;
        }

        chain[0] = anchor;
        for (int i = 1; i<chain.Length; i++) {
            float3 delta = chain[i] - chain[i - 1];
            chain[i] = chain[i - 1] + math.normalize(delta) * boneLength;
        }
    }
}

[AlwaysUpdateSystem]
public class ArmAnimationSystem : JobComponentSystem
{
    public static AnimationCurve throwCurve;
    public EndInitializationEntityCommandBufferSystem ecbSystem;


    struct ArmAnimationJob : IJobForEachWithEntity<ArmComponent, Translation, Rotation>
    {
        public float worldTime;
        
       // [ReadOnly]
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<ArmMatrixBuffer> matrixBufferLookup;

        public EntityCommandBuffer.Concurrent ecb;
        
        public void Execute(Entity entity, int index, ref ArmComponent armComponent, 
            [ReadOnly] ref Translation translation, [ReadOnly] ref Rotation rotation)
        {
            
            //TODO do these chains need to be saved in the arm component so that they keep their position in between animations?
            float3[] armChain;
            float3[][] fingerChains;
            float3[] thumbChain;
            Matrix4x4[] matrices;
            float armBoneLength = armComponent.armBoneLength;
            float3 handUp = armComponent.handUp;
            float armBendStrength = armComponent.armBendStrength;

            // TODO this shouldn't be done here, but we need to save these IK chains on a component?
            armChain = new float3[3];
            fingerChains = new float3[4][];
            for (int i=0;i<fingerChains.Length;i++) {
                fingerChains[i] = new float3[4];
            }
            thumbChain = new float3[4];

            int boneCount = 2;
            for (int i=0;i<fingerChains.Length;i++) {
                boneCount += fingerChains[i].Length - 1;
            }
            boneCount += thumbChain.Length - 1;
		
            matrices = new Matrix4x4[boneCount];
            
            //////////////////////////////////////////
            
            
            // Resting position for hand
            float time = worldTime + armComponent.timeOffset;
            
            // solve the arm IK chain first
            float3 anchor = translation.Value;
            //float3 anchor = new float3();
            FABRIK.Solve(armChain,armBoneLength, anchor, armComponent.handTarget,handUp*armBendStrength);

            Quaternion q = rotation.Value;
            float3 transformRight = math.normalize(math.mul(q, directions.right));
            
            // figure out our current "hand vectors" from our arm orientation
            float3 handForward = math.normalize(util.Last(armChain, 0) - util.Last(armChain, 1));
            handUp = math.normalize(math.cross(handForward,transformRight));
            float3 handRight = math.cross(handUp,handForward);
            
            // create handspace-to-worldspace matrix
            armComponent.handMatrix = Matrix4x4.TRS(util.Last(armChain, 0),Quaternion.LookRotation(handForward,handUp),directions.one);

            // how much are our fingers gripping?
            // (during a reach, this is based on the reach timer)
            float fingerGrabT = armComponent.savedGrabT;
            if (armComponent.heldRock != Entity.Null)
            {
                //Translation heldRockTrans = translationsFromEntity[armComponent.heldRock];
                fingerGrabT = 1.0f; //When holding the rock, we're fully gripped.
                
                 var held = new RockHeldComponent();
                 held.rockInHandPosition = armComponent.handMatrix.MultiplyPoint3x4(armComponent.heldRockOffset);
                 ecb.AddComponent(index, armComponent.heldRock, held);
                //   armComponent.lastIntendedRockPos = 
            }
           
            // create rendering matrices for arm bones
            util.UpdateMatrices(matrices, armChain,0, armComponent.armBoneThickness,handUp);
            int matrixIndex = armChain.Length - 1;

            // next:  fingers
            float3 handPos = util.Last(armChain, 0);
            // fingers spread out during a throw
            float openPalm = throwCurve.Evaluate(armComponent.throwTimer);
            
            //TODO add these to arm component?
            float fingerXOffset = -0.12f;
            float fingerSpacing = 0.08f;
            float[] fingerBoneLengths = { 0.2f, 0.22f, 0.2f, 0.16f };
            float[] fingerThicknesses = {0.05f, 0.05f, 0.05f, 0.05f};
            float fingerBendStrength = 0.2f;

            for (int i=0;i<fingerChains.Length;i++)
            {
                float3 handRightTemp = handRight * (fingerXOffset + i * fingerSpacing);
               // Debug.Log("RJ handRightTemp:"+handRightTemp);
                // find knuckle position for this finger
                float3 fingerPos = handPos + handRightTemp;

                // find resting position for this fingertip
                float3 fingerTarget = fingerPos + handForward * (.5f-.1f*fingerGrabT);

                // spooky finger wiggling while we're idle
                fingerTarget += handUp * Mathf.Sin((time + i*.2f)*3f) * .2f*(1f-fingerGrabT);
			
                // if we're gripping, move this fingertip onto the surface of our rock
                float3 rockFingerDelta = fingerTarget - armComponent.lastIntendedRockPos;
                float3 rockFingerPos = armComponent.lastIntendedRockPos + math.normalize(rockFingerDelta) * (armComponent.lastIntendedRockSize * .5f+fingerThicknesses[i]);
                fingerTarget = math.lerp(fingerTarget,rockFingerPos,fingerGrabT);

                // apply finger-spreading during throw animation
                fingerTarget += (handUp * .3f + handForward * .1f + handRight*(i-1.5f)*.1f) * openPalm;

                // solve this finger's IK chain
                FABRIK.Solve(fingerChains[i],fingerBoneLengths[i],fingerPos,fingerTarget,handUp*fingerBendStrength);

                // update this finger's rendering matrices
                util.UpdateMatrices(matrices, fingerChains[i],matrixIndex,fingerThicknesses[i],handUp);
                matrixIndex += fingerChains[i].Length - 1;
            }
            
            // the thumb is pretty much the same as the fingers
            // (but pointing in a strange direction)
            float thumbXOffset = -0.05f;
            float thumbThickness = 0.06f;
            float thumbBendStrength = 0.1f;
            float thumbBoneLength = 0.13f;

            float3 thumbPos = handPos+handRight*thumbXOffset;
            float3 thumbTarget = thumbPos - handRight * .15f + handForward * (.2f+.1f*fingerGrabT)-handUp*.1f;
            thumbTarget += handRight * Mathf.Sin(time*3f + .5f) * .1f*(1f-fingerGrabT);
            // thumb bends away from the palm, instead of "upward" like the fingers
            float3 thumbBendHint = (-handRight - handForward * .5f);

            float3 rockThumbDelta = thumbTarget - armComponent.lastIntendedRockPos;
            float3 rockThumbPos = armComponent.lastIntendedRockPos + math.normalize(rockThumbDelta) * (armComponent.lastIntendedRockSize * .5f);
            thumbTarget = math.lerp(thumbTarget,rockThumbPos,fingerGrabT);

            FABRIK.Solve(thumbChain,thumbBoneLength,thumbPos,thumbTarget,thumbBendHint * thumbBendStrength);
            util.UpdateMatrices(matrices, thumbChain,matrixIndex,thumbThickness,thumbBendHint);
       
            DynamicBuffer<ArmMatrixBuffer> buffer = matrixBufferLookup[entity];
            buffer.Clear();
            for (int i = 0; i < matrices.Length; i++)
            { 
                buffer.Add(new ArmMatrixBuffer() {Value = matrices[i]});
            }
        }
    }
    
    protected override void OnCreate()
    {
        ecbSystem =  World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        GlobalData gd = util.GetGlobalData();
        if (!gd)
        {
            Debug.LogError("NO GLOBAL DATA");
        }
        else
        {
            throwCurve = gd.armThrowingCurve;
        }
        
        var handle = inputDeps;

        var job = new ArmAnimationJob()
        {
            worldTime = Time.time,
            matrixBufferLookup = GetBufferFromEntity<ArmMatrixBuffer>(),
            ecb = ecbSystem.CreateCommandBuffer().ToConcurrent()
        };
        handle = job.Schedule(this, inputDeps);
        ecbSystem.AddJobHandleForProducer(handle);
        return handle;
    }
}

public class ArmAnimationSystemMainThread : ComponentSystem
{
    private Matrix4x4[] matrices;


    protected override void OnCreate()
    {
        matrices = new Matrix4x4[18];
    }
    
    protected override void OnUpdate()
    {
        if (!util.GetGlobalData())
            return;
        
        Entities.ForEach((Entity entity, ref ArmComponent armComponent) =>
        {
            DynamicBuffer<ArmMatrixBuffer> buffer = EntityManager.GetBuffer<ArmMatrixBuffer>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                matrices[i] = buffer[i].Value;
            }

           Graphics.DrawMeshInstanced(util.GetGlobalData().armMesh ,0, util.GetGlobalData().armMaterial, matrices);
        });
    }
}

/*
public class ArmInitSystem : JobComponentSystem
{
    
    struct ArmInitJob : IJobForEachWithEntity<>
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
    }
}
*/

