using System;
using System.Linq;
using BaseBuilding.Scripts.Managers.ResourceExtractorManager;
using BaseBuilding.scripts.singletons;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.Scripts.Systems.EnergySystem.Wire;
using BaseBuilding.Scripts.Util;
using BaseBuilding.Scripts.Util.Extensions;
using BaseBuilding.Scripts.WorldResources.util;
using Godot;

namespace BaseBuilding.Scripts.WorldResources;

public partial class ResourceExtractor : Node3D, IResourceExtractor
{
    [Export] private float _extractionRate;
    [Export] protected WorldResource Resource = null!;
    [Export] private WireInputConnector _wireInputConnector = null!;
    [Export] private float _energyConsumptionPerExtraction = 1f;
    private double _gameTimeStamp;
    private ThrottledGenerator _throttledGenerator = null!;
    protected ResourceDeposit.ResourceDeposit? ResourceDeposit { get; private set; }
    protected Area3D? ResourceDetector;


    protected void AssignResourceDeposit(ResourceDeposit.ResourceDeposit? resourceDeposit)
    {
        ResourceDeposit = resourceDeposit;
    }

    protected float Extract(float amount)
    {
        if (ResourceDeposit == null) return 0f;
        if (!_wireInputConnector.IsConnected())
        {
            // TODO: Display warning on user interface when there is no energy input
            return 0f;
        }

        var energyReceived = _wireInputConnector.RequestResource(_energyConsumptionPerExtraction);
        if (energyReceived < _energyConsumptionPerExtraction) return 0;
        var amountAvailable = _throttledGenerator.Generate(amount, Global.Instance.GameTimeInSeconds);
        var amountExtracted = ResourceDeposit.Take(amountAvailable);
        return amountExtracted;
    }

    public override void _Ready()
    {
        if (Resource == null) throw new Exception("ResourceExtractor: Resource not assigned!");
        if (_wireInputConnector == null) throw new Exception("ResourceExtractor: Wire input connector not assigned!");

        _gameTimeStamp = Global.Instance.GameTimeInSeconds;
        _throttledGenerator = new ThrottledGenerator(_extractionRate, Global.Instance.GameTimeInSeconds);
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
        _spawnResourceDepositDetector();
    }

    private void _spawnResourceDepositDetector()
    {
        ResourceDetector = new Area3D();
        ResourceDetector.SetCollisionLayerValue(1, false);
        ResourceDetector.SetCollisionMaskValue(1, false);
        ResourceDetector.SetCollisionMaskValue(CollisionIndexes.ResourceDeposit, true);
        var collisionShape = new CollisionShape3D();
        var shape = new SphereShape3D();
        shape.Radius = 1f;
        collisionShape.Shape = shape;
        ResourceDetector.AddChild(collisionShape);
        AddChild(ResourceDetector);
    }

    private void _assignResourceDeposit()
    {
        var overlappingAreas = ResourceDetector!.GetOverlappingAreas();
        var resourceDeposit = (ResourceDeposit.ResourceDeposit?)overlappingAreas.FirstOrDefault(
            area => area is ResourceDeposit.ResourceDeposit deposit && deposit.Resource.Id == Resource.Id
        );
        AssignResourceDeposit(resourceDeposit);
    }

    private async void _activate()
    {
        // Wait for the next frame so that the resource deposit detector has time to collect collision information in
        // the case that we are loading from a save.
        await this.WaitForNextFrame();
        _wireInputConnector.Activate();
        _assignResourceDeposit();
        ResourceDetector?.QueueFree();
    }

    public override void _ExitTree()
    {
        var building = GetParent<Building>();
        building.PlacedEvent -= _activate;
    }

    public bool HasResource(WorldResource resource)
    {
        throw new NotImplementedException();
    }
}