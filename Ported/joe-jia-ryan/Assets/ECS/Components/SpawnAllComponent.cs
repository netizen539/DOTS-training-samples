using Unity.Entities;

public struct SpawnAllComponent : IComponentData
{
    public Entity RockPrefab;
    public Entity TinCanPrefab;
    public int Count;

}