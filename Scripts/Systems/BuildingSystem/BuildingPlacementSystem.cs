using BaseBuilding.scripts.singletons;
using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public partial class BuildingPlacementSystem : Node
{
    private readonly BaseMaterial3D _placementMaterial = new StandardMaterial3D();
    private Global _global = null!;
    private Building? _preview;
    private BuildingResource? _resource;
    private StatusEnum _status = StatusEnum.Inactive;

    public override void _Ready()
    {
        SetProcess(false);
        SetProcessUnhandledInput(false);
        _global = GetNode<Global>("/root/Global");
        _placementMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
    }

    public override void _Process(double delta)
    {
        if (_status == StatusEnum.Placing)
        {
            _preview!.GlobalPosition = _global.MousePositionInWorld;
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
                    foreach (var previewMeshInstance in _preview.MeshInstance3Ds)
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
        _preview.Position = _global.MousePositionInWorld;
        foreach (var previewMeshInstance in _preview.MeshInstance3Ds)
            previewMeshInstance.MaterialOverlay = _placementMaterial;
        AddChild(_preview);
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