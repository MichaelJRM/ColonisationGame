using System;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.LiquidResourceStorage;

public partial class LiquidResourceStorage : ResourceStorage
{
    [Export] private PipeInputConnector[] _pipeInputConnectors = Array.Empty<PipeInputConnector>();
    [Export] private PipeOutputConnector[] _pipeOutputConnectors = Array.Empty<PipeOutputConnector>();


    public override void _Ready()
    {
        _validate();
        base._Ready();
    }

    protected override PipeInputConnector[] GetInputConnectors() => _pipeInputConnectors;

    protected override PipeOutputConnector[] GetOutputConnectors() => _pipeOutputConnectors;

    private void _validate()
    {
        if (_pipeInputConnectors.Length == 0)
            throw new Exception("LiquidResourceStorage: Pipe input connectors not assigned!");

        if (_pipeOutputConnectors.Length == 0)
            throw new Exception("LiquidResourceStorage: Pipe output connectors not assigned!");
    }
}