using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ArmComponent : IComponentData
{
    public float3 handUp;
    public float maxReachLength;
    public float reachingTimer;
    public float reachDuration;
    public float3 handTarget;
    public float armBoneLength;
    public float armBendStrength;
    public float armBoneThickness;
    public float timeOffset;

    public float3 lastIntendedRockPos;
    public float lastIntendedRockSize;

}
