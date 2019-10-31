using Unity.Entities;

public struct SpawnAllComponent : IComponentData
{
    public Entity RockPrefab;
    public Entity TinCanPrefab;
    public Entity ArmPrefab;
    public int Count;

}