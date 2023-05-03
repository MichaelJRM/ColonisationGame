using System.Globalization;
using System.Linq;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using BaseBuilding.Scripts.Util;
using Godot;
using Godot.Collections;

namespace BaseBuilding.Scripts.WorldResources.LiquidResourceExtractor;

public partial class LiquidResourceExtractor : ResourceExtractor
{
    [Export] private PipeOutputConnector[] _pipeOutputConnectors = System.Array.Empty<PipeOutputConnector>();
    private Area3D _resourceDetector = null!;

    public override void _Ready()
    {
        base._Ready();
        _spawnResourceDepositDetector();
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
        building.IsPlacementValidCallbacks.Add(_doesHaveRequiredResource);
    }

    private void _spawnResourceDepositDetector()
    {
        _resourceDetector = new Area3D();
        _resourceDetector.SetCollisionLayerValue(1, false);
        _resourceDetector.SetCollisionMaskValue(1, false);
        _resourceDetector.SetCollisionMaskValue(CollisionIndexes.ResourceDeposit, true);
        var collisionShape = new CollisionShape3D();
        var shape = new SphereShape3D();
        shape.Radius = 1f;
        collisionShape.Shape = shape;
        _resourceDetector.AddChild(collisionShape);
        AddChild(_resourceDetector);
    }

    private void _activate()
    {
        _resourceDetector.QueueFree();
        _activatePipeConnectors();
    }

    private void _activatePipeConnectors()
    {
        foreach (var pipeConnector in _pipeOutputConnectors)
        {
            pipeConnector.BindOnResourceAsked(_onResourceAsked);
            pipeConnector.Activate();
        }
    }

    private float _onResourceAsked(float amount)
    {
        var amountExtracted = Extract(amount);
        if (amountExtracted <= 0) return 0;

        GetNode<Label3D>("Label3D").Text = ResourceDeposit!.CurrentAmount.ToString(CultureInfo.CurrentCulture);
        return amountExtracted;
    }

    private bool _doesHaveRequiredResource()
    {
        var overlappingAreas = _resourceDetector.GetOverlappingAreas();
        ResourceDeposit = (ResourceDeposit.ResourceDeposit?)overlappingAreas.FirstOrDefault(area =>
            area is ResourceDeposit.ResourceDeposit deposit && deposit.Resource.Id == Resource.Id);
        return ResourceDeposit != null;
    }
}