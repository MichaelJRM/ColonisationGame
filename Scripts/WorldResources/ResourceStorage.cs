using System;
using System.Globalization;
using BaseBuilding.scripts.common;
using BaseBuilding.Scripts.Systems;
using BaseBuilding.scripts.systems.BuildingSystem;
using Godot;

namespace BaseBuilding.Scripts.WorldResources;

public partial class ResourceStorage : Node
{
    [Export] private float _capacity = 1000f;
    [Export] private int _inputRateInSeconds = 1;
    [Export] private Label3D _debugStorageLabel;
    [Export] private WorldResource _resource = null!;

    private float _currentAmount;
    private TickComponent _inputTick = new();

    protected virtual IResourceInputConnector[] GetInputConnectors()
    {
        throw new NotImplementedException();
    }

    protected virtual IResourceOutputConnector[] GetOutputConnectors()
    {
        throw new NotImplementedException();
    }

    public override void _Ready()
    {
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
    }

    private void _activate()
    {
        _activateInputConnectors();
        _initInputTick();
    }


    private void _initInputTick()
    {
        _inputTick.SetTickRateInSeconds(_inputRateInSeconds);
        _inputTick.SetOnTick(_handleInput);
        AddChild(_inputTick);
    }

    private void _activateInputConnectors()
    {
        foreach (var pipeInputConnector in GetInputConnectors()) pipeInputConnector.Activate();
        foreach (var pipeOutputConnector in GetOutputConnectors())
        {
            pipeOutputConnector.BindOnResourceAsked(_take);
            pipeOutputConnector.Activate();
        }
    }


    private float _add(float amount)
    {
        var newAmount = _currentAmount + amount;
        var amountAdded = newAmount - _currentAmount;
        _currentAmount = newAmount;
        _debugStorageLabel.Text = _currentAmount.ToString(CultureInfo.CurrentCulture);
        return amountAdded;
    }

    private float _take(float amount)
    {
        var newAmount = Mathf.Max(_currentAmount - amount, 0f);
        var amountRemoved = _currentAmount - newAmount;
        _currentAmount = newAmount;
        _debugStorageLabel.Text = _currentAmount.ToString(CultureInfo.CurrentCulture);
        return amountRemoved;
    }

    private void _handleInput()
    {
        if (_currentAmount >= _capacity) return;
        foreach (var inputConnector in GetInputConnectors())
        {
            if (inputConnector.IsConnectedToLine())
            {
                _add(inputConnector.RequestResource(_resource));
            }
        }
    }
}