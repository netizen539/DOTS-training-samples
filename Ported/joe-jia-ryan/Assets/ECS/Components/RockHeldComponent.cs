using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct RockHeldComponent : IComponentData
{
    public float3 rockInHandPosition;
}
