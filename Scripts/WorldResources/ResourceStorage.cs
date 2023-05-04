using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseBuilding.scripts.common;
using BaseBuilding.Scripts.Systems;
using BaseBuilding.scripts.systems.BuildingSystem;
using Godot;
using Godot.Collections;
using Array = System.Array;

namespace BaseBuilding.Scripts.WorldResources;

[Tool]
public partial class ResourceStorage : Node
{
    [Export] private float _capacity = 1000f;
    [Export] private int _inputRateInSeconds = 1;
    [Export] private int _inputRateAmount = 1;

    private Label3D? _debugStorageLabel;

    [Export]
    private Label3D? DebugStorageLabel
    {
        get => _debugStorageLabel;
        set
        {
            _debugStorageLabel = value;
            UpdateConfigurationWarnings();
        }
    }

    // Workaround while Godot doesn't support Interface exports.
    private Godot.Collections.Array<Area3D> _resourceInputConnectors = new();

    [Export]
    private Array<Area3D> ResourceInputConnectors
    {
        get => _resourceInputConnectors;
        set
        {
            _resourceInputConnectors = value;
            UpdateConfigurationWarnings();
        }
    }

    private IResourceInputConnector[] _iResourceInputConnectors = Array.Empty<IResourceInputConnector>();
    private Godot.Collections.Array<Area3D> _resourceOutputConnectors = new();

    [Export]
    private Array<Area3D> ResourceOutputConnectors
    {
        get => _resourceOutputConnectors;
        set
        {
            _resourceOutputConnectors = value;
            UpdateConfigurationWarnings();
        }
    }

    private IResourceOutputConnector[] _iResourceOutputConnectors = Array.Empty<IResourceOutputConnector>();
    // ----------------------------------------------------------

    private float _currentAmount;
    private TickComponent _inputTick = new();

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;

        _validate();
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;

        // Workaround while Godot doesn't support Interface exports.
        _iResourceInputConnectors = _resourceInputConnectors.Cast<IResourceInputConnector>().ToArray();
        _iResourceOutputConnectors = _resourceOutputConnectors.Cast<IResourceOutputConnector>().ToArray();
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
        foreach (var pipeInputConnector in _iResourceInputConnectors) pipeInputConnector.Activate();
        foreach (var pipeOutputConnector in _iResourceOutputConnectors)
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
        if (_debugStorageLabel != null)
        {
            _debugStorageLabel.Text = _currentAmount.ToString(CultureInfo.CurrentCulture);
        }

        return amountAdded;
    }

    private float _take(float amount)
    {
        var newAmount = Mathf.Max(_currentAmount - amount, 0f);
        var amountRemoved = _currentAmount - newAmount;
        _currentAmount = newAmount;
        if (_debugStorageLabel != null)
        {
            _debugStorageLabel.Text = _currentAmount.ToString(CultureInfo.CurrentCulture);
        }

        return amountRemoved;
    }

    private void _handleInput()
    {
        if (_currentAmount >= _capacity) return;
        foreach (var inputConnector in _iResourceInputConnectors)
        {
            if (inputConnector.IsConnectedToLine())
            {
                _add(inputConnector.RequestResource(_inputRateAmount));
            }
        }
    }


    private void _validate()
    {
        if (_debugStorageLabel == null) throw new Exception("ResourceStorage: Debug storage label not assigned!");
        if (_resourceInputConnectors.Count == 0)
            throw new Exception("ResourceStorage: Input connectors not assigned!");
        if (_resourceOutputConnectors.Count == 0)
            throw new Exception("ResourceStorage: Output connectors not assigned!");
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint()) return;

        var building = GetParent<Building>();
        building.PlacedEvent -= _activate;
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new List<string>();

        if (_debugStorageLabel == null)
        {
            warnings.Add("Debug storage label not assigned!");
        }

        if (_resourceInputConnectors.Count == 0)
        {
            warnings.Add("Input connectors not assigned!");
        }

        if (_resourceOutputConnectors.Count == 0)
        {
            warnings.Add("Output connectors not assigned!");
        }

        return warnings.ToArray();
    }
}