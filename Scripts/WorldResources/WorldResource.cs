using Godot;

namespace BaseBuilding.Scripts.WorldResources;

public partial class WorldResource : Resource
{
    [Export] public string Id { get; private set; } = "WorldResourceID";
    [Export] public string Name { get; private set; } = "WorldResourceName";
}