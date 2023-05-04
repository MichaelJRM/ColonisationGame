using System;
using BaseBuilding.Scripts.Systems;
using BaseBuilding.Scripts.Systems.EnergySystem.Wire;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.EnergyResourceStorage;

public partial class EnergyResourceStorage : ResourceStorage
{
    [Export] private WireInputOutputConnector[] _wireInputOutputConnectors = Array.Empty<WireInputOutputConnector>();

    public override void _Ready()
    {
        base._Ready();
        if (_wireInputOutputConnectors.Length == 0)
            throw new Exception("EnergyResourceStorage: Wire input/output connectors not assigned!");
    }


    protected override IResourceInputConnector[] GetInputConnectors() => _wireInputOutputConnectors;

    protected override IResourceOutputConnector[] GetOutputConnectors() => _wireInputOutputConnectors;
}