using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public sealed partial class BuildingSystem : Node
{
    private BuildingPlacementSystem _placementSystem = new();

    private BuildingSystem()
    {
    }

    public static BuildingSystem Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        AddChild(_placementSystem);
    }

    public void StartBuildingPlacement(BuildingResource buildingResource)
    {
        _placementSystem.StartBuildingPlacement(buildingResource);
    }
}