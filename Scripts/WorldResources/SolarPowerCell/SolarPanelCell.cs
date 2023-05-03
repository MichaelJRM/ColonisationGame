using System;
using BaseBuilding.scripts.singletons;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.Scripts.Systems.EnergySystem.Wire;
using BaseBuilding.Scripts.WorldResources.util;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.SolarPowerCell;

public partial class SolarPanelCell : Node
{
    /// <summary>
    /// The amount of energy that gets generated every game second.
    /// </summary>
    [Export] private float _generationRatePerSecond;

    [Export] private WireOutputConnector[] _wireOutputConnectors = Array.Empty<WireOutputConnector>();

    private ThrottledGenerator _throttledGenerator = null!;


    public override void _Ready()
    {
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
        _throttledGenerator = new ThrottledGenerator(_generationRatePerSecond, Global.Instance.GameTimeInSeconds);
    }

    private void _activate()
    {
        _activateConnectors();
    }

    private void _activateConnectors()
    {
        foreach (var wireOutputConnector in _wireOutputConnectors)
        {
            wireOutputConnector.BindOnResourceAsked(_onResourceAsked);
            wireOutputConnector.Activate();
        }
    }

    private float _onResourceAsked(float amount)
    {
        return _throttledGenerator.Generate(amount, Global.Instance.GameTimeInSeconds);
    }
}