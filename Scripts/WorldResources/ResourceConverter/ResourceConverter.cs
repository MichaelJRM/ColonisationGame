using System;
using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.common;
using BaseBuilding.Scripts.Systems;
using BaseBuilding.scripts.systems.BuildingSystem;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.ResourceConverter;

public partial class ResourceConverter : Node
{
    [Export] private int _conversionRate = 1;

    [Export] private int _inputRate = 1;

    // Workaround while Godot doesn't support Interface exports.
    [Export] private Area3D[] __resourceInputConnectors = Array.Empty<Area3D>();
    protected IResourceInputConnector[] _resourceInputConnectors = Array.Empty<IResourceInputConnector>();
    [Export] private Area3D[] __resourceOutputConnectors = Array.Empty<Area3D>();
    protected IResourceOutputConnector[] _resourceOutputConnectors = Array.Empty<IResourceOutputConnector>();

    // ----------------------------------------------------------
    [Export] private ResourceConversionData[] _resourceConversionData = Array.Empty<ResourceConversionData>();
    [Export] private ResourceStorageData[] _resourceStorageData = Array.Empty<ResourceStorageData>();
    private readonly TickComponent _conversionTick = new();
    private readonly Dictionary<string, float> _inputResourceStorageAmount = new();
    private readonly Dictionary<string, float> _inputResourceStorageCapacity = new();
    private readonly TickComponent _inputTick = new();
    private readonly Dictionary<string, float> _outputResourceStorageAmount = new();
    private readonly Dictionary<string, float> _outputResourceStorageCapacity = new();


    public override void _Ready()
    {
        _validate();
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;

        // Workaround while Godot doesn't support Interface exports.
        _resourceInputConnectors = Array.ConvertAll(__resourceInputConnectors, e => (IResourceInputConnector)e);
        _resourceOutputConnectors = Array.ConvertAll(__resourceOutputConnectors, e => (IResourceOutputConnector)e);
    }

    private void _activate()
    {
        _initResourceData();
        _activateConnectors();
        _initTickComponents();
    }

    private void _initResourceData()
    {
        foreach (var conversionData in _resourceConversionData)
        {
            _inputResourceStorageAmount[conversionData.InputResource.Id] = 0f;
            var inputStorageData = _resourceStorageData.First(e => e.Resource.Id == conversionData.InputResource.Id);
            _inputResourceStorageCapacity[conversionData.InputResource.Id] = inputStorageData.Capacity;
            _outputResourceStorageAmount[conversionData.OutputResource.Id] = 0f;
            var outputStorageData = _resourceStorageData.First(e => e.Resource.Id == conversionData.OutputResource.Id);
            _outputResourceStorageCapacity[conversionData.OutputResource.Id] = outputStorageData.Capacity;
        }
    }

    private void _activateConnectors()
    {
        foreach (var inputConnector in _resourceInputConnectors) inputConnector.Activate();
        foreach (var conversionData in _resourceConversionData)
        {
            foreach (var connector in _resourceOutputConnectors)
            {
                if (connector.AcceptsResource(conversionData.OutputResource))
                {
                    connector.BindOnResourceAsked(amount => _take(amount, conversionData.OutputResource));
                    connector.Activate();
                }
            }
        }
    }

    private void _initTickComponents()
    {
        _inputTick.SetTickRateInFps(_inputRate);
        _inputTick.SetOnTick(_handleResourceInput);
        AddChild(_inputTick);
        _conversionTick.SetTickRateInFps(_conversionRate);
        _conversionTick.SetOnTick(_handleResourceConversion);
        AddChild(_conversionTick);
    }

    private float _add(float amount, WorldResource resource)
    {
        var storageAmount = _inputResourceStorageAmount[resource.Id];
        var newAmount = storageAmount + amount;
        var amountAdded = newAmount - storageAmount;
        _inputResourceStorageAmount[resource.Id] = newAmount;
        return amountAdded;
    }

    private float _take(float amount, WorldResource resource)
    {
        var storageAmount = _outputResourceStorageAmount[resource.Id];
        var newAmount = Mathf.Max(storageAmount - amount, 0f);
        var amountRemoved = storageAmount - newAmount;
        _outputResourceStorageAmount[resource.Id] = newAmount;
        return amountRemoved;
    }


    private void _handleResourceInput()
    {
        foreach (var conversionData in _resourceConversionData)
        {
            var storageAmount = _inputResourceStorageAmount[conversionData.InputResource.Id];
            var capacityAmount = _inputResourceStorageCapacity[conversionData.InputResource.Id];
            if (storageAmount >= capacityAmount) continue;
            foreach (var connector in _resourceInputConnectors)
            {
                if (connector.IsConnectedToLine() && connector.AcceptsResource(conversionData.InputResource))
                {
                    _add(connector.RequestResource(conversionData.InputAmount), conversionData.InputResource);
                }
            }
        }
    }

    private void _handleResourceConversion()
    {
        var howManyUnitsCanBeMadeOverall = int.MaxValue;
        foreach (var conversionData in _resourceConversionData)
        {
            var outputStorageAmount = _outputResourceStorageAmount[conversionData.OutputResource.Id];
            var outputStorageCapacity = _outputResourceStorageCapacity[conversionData.OutputResource.Id];

            // If the output storage is full, we can't make any more units
            if (outputStorageAmount >= outputStorageCapacity) return;
        }

        foreach (var conversionData in _resourceConversionData)
        {
            var availableAmount = _inputResourceStorageAmount[conversionData.InputResource.Id];
            var howManyUnitsCanBeMadeFromThisResource =
                Mathf.Max(Mathf.FloorToInt(availableAmount / conversionData.InputAmount), 0);

            if (howManyUnitsCanBeMadeFromThisResource < howManyUnitsCanBeMadeOverall)
                howManyUnitsCanBeMadeOverall = howManyUnitsCanBeMadeFromThisResource;
        }

        foreach (var conversionData in _resourceConversionData)
        {
            var outputAmount = conversionData.OutputAmount * howManyUnitsCanBeMadeOverall;
            _inputResourceStorageAmount[conversionData.InputResource.Id] -=
                conversionData.InputAmount * howManyUnitsCanBeMadeOverall;
            _outputResourceStorageAmount[conversionData.OutputResource.Id] += outputAmount;
        }
    }

    private void _validate()
    {
        if (__resourceInputConnectors.Length == 0)
            throw new Exception("ResourceConverter: Resource input connectors not assigned!");
        if (__resourceOutputConnectors.Length == 0)
            throw new Exception("ResourceConverter: Resource output connectors not assigned!");
        if (_resourceConversionData.Length == 0)
            throw new Exception("ResourceConverter: Resource conversion data not assigned!");
        if (_resourceStorageData.Length == 0)
            throw new Exception("ResourceConverter: Resource storage data not assigned!");
    }

    public override void _ExitTree()
    {
        var building = GetParent<Building>();
        building.PlacedEvent -= _activate;
    }
}