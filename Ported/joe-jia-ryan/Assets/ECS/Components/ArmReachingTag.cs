using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ArmReachingTag : IComponentData
{
    public Entity rockToReachFor;
}
