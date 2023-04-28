using BaseBuilding.scripts.singletons;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.Scripts.Systems.EnergySystem.WireConnector;
using BaseBuilding.Scripts.WorldResources.util;
using Godot;
using Godot.Collections;

namespace BaseBuilding.Scripts.WorldResources;

public partial class SolarPanelCell : Node
{
    [Export] private float _generationRatePerSecond;
    private Global _global = null!;
    private ThrottledGenerator _throttledGenerator = null!;
    [Export] private Array<WireOutputConnector> _wireOutputConnectors = new();


    public override void _Ready()
    {
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
        _global = GetNode<Global>("/root/Global");
        _throttledGenerator = new ThrottledGenerator(_generationRatePerSecond, _global.GameTimeInSeconds);
    }

    private void _activate()
    {
        _activateConnectors();
    }

    private void _activateConnectors()
    {
        foreach (var wireOutputConnector in _wireOutputConnectors) wireOutputConnector.Activate(_onResourceAsked);
    }

    private float _onResourceAsked(float amount)
    {
        return _throttledGenerator.Generate(amount, _global.GameTimeInSeconds);
    }
}