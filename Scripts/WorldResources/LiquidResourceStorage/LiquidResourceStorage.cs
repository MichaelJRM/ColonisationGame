using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using BaseBuilding.scripts.common;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using Godot;
using Godot.Collections;

namespace BaseBuilding.Scripts.WorldResources.LiquidResourceStorage;

public partial class LiquidResourceStorage : Node
{
    [Export] private float _capacity = 1000f;
    private float _currentAmount;
    [Export] private float _inputRate = 1f;
    private TickComponent _inputTick = new();
    private Label3D _label = null!;
    [Export] private Array<PipeInputConnector> _pipeInputConnectors = new();
    [Export] private Array<PipeOutputConnector> _pipeOutputConnectors = new();
    [Export] private WorldResource _resource = null!;

    public override void _Ready()
    {
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
        _label = building.GetNode<Label3D>("Label3D");
    }

    private void _activate()
    {
        _activateInputConnectors();
        _initInputTick();
    }


    private void _initInputTick()
    {
        _inputTick.SetTickRate(_inputRate);
        _inputTick.SetOnTick(_handleInput);
        AddChild(_inputTick);
    }

    private void _activateInputConnectors()
    {
        foreach (var pipeInputConnector in _pipeInputConnectors) pipeInputConnector.Activate();
        foreach (var pipeOutputConnector in _pipeOutputConnectors) pipeOutputConnector.Activate(_take);
    }


    private float _add(float amount)
    {
        var newAmount = _currentAmount + amount;
        var amountAdded = newAmount - _currentAmount;
        _currentAmount = newAmount;
        _label.Text = _currentAmount.ToString(CultureInfo.CurrentCulture);
        return amountAdded;
    }

    private float _take(float amount)
    {
        var newAmount = Mathf.Max(_currentAmount - amount, 0f);
        var amountRemoved = _currentAmount - newAmount;
        _currentAmount = newAmount;
        _label.Text = _currentAmount.ToString(CultureInfo.CurrentCulture);
        return amountRemoved;
    }

    private void _handleInput()
    {
        if (_currentAmount >= _capacity) return;
        _pipeInputConnectors
            .Where(e => e.IsConnectedToOtherJoints())
            .ToImmutableList()
            .ForEach(e => _add(e.RequestResource(_resource)));
    }
}