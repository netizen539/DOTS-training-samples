using Unity.Entities;

public struct SpawnAllComponent : IComponentData
{
    public Entity Prefab;
    public int Count;
}