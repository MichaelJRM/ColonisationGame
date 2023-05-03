using System;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.LiquidResourceStorage;

public partial class LiquidResourceStorage : ResourceStorage
{
    [Export] private PipeInputConnector[] _pipeInputConnectors = Array.Empty<PipeInputConnector>();
    [Export] private PipeOutputConnector[] _pipeOutputConnectors = Array.Empty<PipeOutputConnector>();


    protected override PipeInputConnector[] GetInputConnectors() => _pipeInputConnectors;

    protected override PipeOutputConnector[] GetOutputConnectors() => _pipeOutputConnectors;
}