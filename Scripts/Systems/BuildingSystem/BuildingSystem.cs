using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public partial class BuildingSystem : Node
{
    private BuildingPlacementSystem _placementSystem = new();

    public override void _Ready()
    {
        AddChild(_placementSystem);
    }

    public void StartBuildingPlacement(BuildingResource buildingResource)
    {
        _placementSystem.StartBuildingPlacement(buildingResource);
    }
}