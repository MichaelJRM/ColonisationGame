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
    protected ResourceDeposit.ResourceDeposit? ResourceDeposit;


    public override void _Ready()
    {
        if (Resource == null) throw new Exception("ResourceExtractor: Resource is null!");
        _gameTimeStamp = Global.Instance.GameTimeInSeconds;
        _throttledGenerator = new ThrottledGenerator(_extractionRate, Global.Instance.GameTimeInSeconds);
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
    }

    private void _activate()
    {
        _wireInputConnector.Activate();
    }

    protected float Extract(float amount)
    {
        if (ResourceDeposit == null) return 0f;

        var energyReceived = _wireInputConnector.RequestResource(_energyConsumptionPerExtraction);
        if (energyReceived < _energyConsumptionPerExtraction) return 0;
        var amountAvailable = _throttledGenerator.Generate(amount, Global.Instance.GameTimeInSeconds);
        var amountExtracted = ResourceDeposit.Take(amountAvailable);
        return amountExtracted;
    }
}