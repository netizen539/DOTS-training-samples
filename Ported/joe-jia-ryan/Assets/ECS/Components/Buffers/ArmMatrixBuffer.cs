using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[InternalBufferCapacity(18)]
public struct ArmMatrixBuffer : IBufferElementData
{
    public Matrix4x4 Value;
}
