using Godot;

namespace BaseBuilding.Scripts.WorldResources;

public partial class ResourceConversionData : Resource
{
    [Export] public uint InputAmount;
    [Export] public WorldResource InputResource = null!;
    [Export] public uint OutputAmount;
    [Export] public WorldResource OutputResource = null!;
}