using System;
using System.Collections.Generic;
using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

[Tool]
public partial class BuildingCollisionArea : Area3D
{
    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        var building = GetParent<Building>();
        building.PlacedEvent += _onBuildingPlaced;
    }

    private void _onBuildingPlaced()
    {
        Monitorable = false;
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint()) return;
        var building = GetParent<Building>();
        building.PlacedEvent -= _onBuildingPlaced;
    }

    private void _validate()
    {
        if (GetParent() is not Building)
        {
            throw new Exception("BuildingCollisionArea has to be a child of Building!");
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new List<string>();
        if (GetParent() is not Building)
        {
            warnings.Add("BuildingCollisionArea has to be a child of Building!");
        }

        return warnings.ToArray();
    }
}