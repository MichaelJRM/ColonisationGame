﻿using BaseBuilding.scripts.singletons;
using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public partial class BuildingPlacementSystem : Node
{
    private readonly BaseMaterial3D _placementMaterial = new StandardMaterial3D();
    private Building? _preview;
    private BuildingResource? _resource;
    private StatusEnum _status = StatusEnum.Inactive;
    private Node _context = null!;

    public BuildingPlacementSystem(Node context)
    {
        _context = context;
    }

    public override void _Ready()
    {
        SetProcess(false);
        SetProcessUnhandledInput(false);
        _placementMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
    }

    public override void _Process(double delta)
    {
        if (_status == StatusEnum.Placing)
        {
            _preview!.GlobalPosition = Global.Instance.GetMousePositionInWorld();
            var isPlacementValid = _preview.IsPlacementValid();
            _placementMaterial.AlbedoColor = isPlacementValid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_status == StatusEnum.Placing)
        {
            if (@event.IsActionPressed("build_manager_cancel_placement"))
            {
                _preview!.QueueFree();
                _preview = null;
                _status = StatusEnum.Active;
                SetProcess(false);
                SetProcessUnhandledInput(false);
            }
            else if (@event.IsActionPressed("build_manager_place_item"))
            {
                var isPlacementValid = _preview!.IsPlacementValid();
                if (isPlacementValid)
                {
                    _preview.OnPlaced();
                    foreach (var previewMeshInstance in _preview.MeshInstances)
                    {
                        previewMeshInstance.MaterialOverlay = null;
                    }

                    _preview = null;
                    _status = StatusEnum.Active;
                    SetProcess(false);
                    SetProcessUnhandledInput(false);
                }
            }
        }
    }

    public void StartBuildingPlacement(BuildingResource buildingResource)
    {
        _preview?.QueueFree();
        _resource = buildingResource;
        _preview = buildingResource.Scene.Instantiate<Building>();
        _preview.Position = Global.Instance.GetMousePositionInWorld();
        foreach (var previewMeshInstance in _preview.MeshInstances)
            previewMeshInstance.MaterialOverlay = _placementMaterial;
        _context.AddChild(_preview);
        _status = StatusEnum.Placing;
        SetProcess(true);
        SetProcessUnhandledInput(true);
    }


    private enum StatusEnum
    {
        Inactive,
        Active,
        Placing
    }
}