using Godot;

namespace BaseBuilding.Scripts.WorldResources;

public partial class ResourceStorageData : Resource
{
    [Export] public float Capacity;
    [Export] public WorldResource Resource = null!;
}