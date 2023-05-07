using System.Linq;
using BaseBuilding.Scripts.Systems.SaveSystem;
using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public sealed partial class BuildingSystem : Node, IPersistentManager
{
    private BuildingPlacementSystem _placementSystem = null!;

    private BuildingSystem()
    {
    }

    public static BuildingSystem Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        _placementSystem = new(this);
        AddChild(_placementSystem);
    }

    public void StartBuildingPlacement(BuildingResource buildingResource)
    {
        _placementSystem.StartBuildingPlacement(buildingResource);
    }


    public void _AddSaveChild(Node child)
    {
        AddChild(child);
    }

    public IPersistent[] _GetPersistentChildren()
    {
        return GetChildren().OfType<IPersistent>().ToArray();
    }

    public string _GetSavePath()
    {
        return "user://buildingSystem.save";
    }
}