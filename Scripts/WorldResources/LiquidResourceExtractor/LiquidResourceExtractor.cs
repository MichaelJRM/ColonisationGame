﻿using System.Globalization;
using System.Linq;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.LiquidResourceExtractor;

public partial class LiquidResourceExtractor : ResourceExtractor
{
    [Export] private PipeOutputConnector[] _pipeOutputConnectors = System.Array.Empty<PipeOutputConnector>();


    public override void _Ready()
    {
        base._Ready();
        var building = GetParent<Building>();
        building.PlacedEvent += _activate;
        building.IsPlacementValidCallbacks.Add(_doesHaveRequiredResource);
    }

    private void _activate()
    {
        _activatePipeConnectors();
    }

    private void _activatePipeConnectors()
    {
        foreach (var pipeConnector in _pipeOutputConnectors)
        {
            pipeConnector.BindOnResourceAsked(_onResourceAsked);
            pipeConnector.Activate();
        }
    }

    private float _onResourceAsked(float amount)
    {
        var amountExtracted = Extract(amount);
        if (amountExtracted <= 0) return 0;

        GetNode<Label3D>("Label3D").Text = ResourceDeposit!.CurrentAmount.ToString(CultureInfo.CurrentCulture);
        return amountExtracted;
    }

    private bool _doesHaveRequiredResource()
    {
        var overlappingAreas = ResourceDetector!.GetOverlappingAreas();
        return overlappingAreas.Any(
            area => area is ResourceDeposit.ResourceDeposit deposit && deposit.Resource.Id == Resource.Id
        );
    }

    public override void _ExitTree()
    {
        var building = GetParent<Building>();
        building.PlacedEvent -= _activate;
    }
}