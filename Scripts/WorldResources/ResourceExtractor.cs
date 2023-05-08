using System;
using BaseBuilding.scripts.singletons;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.Scripts.Systems.EnergySystem.Wire;
using BaseBuilding.Scripts.WorldResources.util;
using Godot;

namespace BaseBuilding.Scripts.WorldResources;

public partial class ResourceExtractor : Node3D
{
    [Export] private float _extractionRate;
    [Export] protected WorldResource Resource = null!;
    [Export] private WireInputConnector _wireInputConnector = null!;
    [Export] private float _energyConsumptionPerExtraction = 1f;
    private double _gameTimeStamp;
    private ThrottledGenerator _throttledGenerator = null!;
    protected ResourceDeposit.ResourceDeposit? ResourceDeposit { get; private set; }


    protected void AssignResourceDeposit(ResourceDeposit.ResourceDeposit? resourceDeposit)
    {
        ResourceDeposit = resourceDeposit;
    }


    public override void _Ready()
    {
        if (Resource == null) throw new Exception("ResourceExtractor: Resource not assigned!");
        if (_wireInputConnector == null) throw new Exception("ResourceExtractor: Wire input connector not assigned!");

        _gameTimeStamp = Global.Instance.GameTimeInSeconds;
        _throttledGenerator = new ThrottledGenerator(_extractionRate, Global.Instance.GameTimeInSeconds);
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
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

    private void _activate()
    {
        _wireInputConnector.Activate();
    }

    public override void _ExitTree()
    {
        var building = GetParent<Building>();
        building.PlacedEvent -= _activate;
    }
}