using System;
using BaseBuilding.Scripts.Systems;
using BaseBuilding.Scripts.Systems.EnergySystem.Wire;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.EnergyResourceStorage;

public partial class EnergyResourceStorage : ResourceStorage
{
    [Export] private WireInputOutputConnector[] _wireInputOutputConnectors = Array.Empty<WireInputOutputConnector>();


    protected override IResourceInputConnector[] GetInputConnectors() => _wireInputOutputConnectors;

    protected override IResourceOutputConnector[] GetOutputConnectors() => _wireInputOutputConnectors;
}