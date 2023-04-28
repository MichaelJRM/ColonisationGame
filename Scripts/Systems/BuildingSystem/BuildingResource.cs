using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public partial class BuildingResource : Resource
{
    [Export] public string Id { get; private set; } = "";
    [Export] public string Name { get; private set; } = "";
    [Export] public PackedScene Scene { get; private set; } = null!;
}