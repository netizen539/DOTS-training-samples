using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(3)]
public struct ArmChainsBuffer : IBufferElementData
{
    public float3 element;
}

[InternalBufferCapacity(16)]
public struct FingerChainsBuffer : IBufferElementData
{
    public float3 element;
}

[InternalBufferCapacity(4)]
public struct ThumbChainsBuffer : IBufferElementData
{
    public float3 element;
}