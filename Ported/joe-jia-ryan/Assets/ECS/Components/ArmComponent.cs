using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct ArmComponent : IComponentData
{
    public float3 handUp;
    public float maxReachLength;
    public float reachingTimer;
    public float reachDuration;
    public float3 grabHandTarget;
    public float3 handTarget;
    public float3 idleHandTarget;
    public float savedGrabT;
    public float armBoneLength;
    public float armBendStrength;
    public float armBoneThickness;
    public float timeOffset;

    public float3 lastIntendedRockPos;
    public float lastIntendedRockSize;
    public Entity heldRock;
    public float throwTimer;
    public float windupTimer;
    public float windupDuration;
    public Entity targetCan;
    public Matrix4x4 handMatrix;
    public float3 heldRockOffset;
    public float3 windupHandTarget;
    public float throwDuration;
    public float3 aimVector;

}
