﻿using System;
using System.Collections.Generic;
using System.Linq;
using BaseBuilding.Scripts.Systems.SaveSystem;
using Godot;


namespace BaseBuilding.scripts.systems.BuildingSystem;

public delegate bool IsPlacementValidCallback();

[Tool]
public partial class Building : PersistentNode3D
{
    private BuildingCollisionArea _collisionArea = null!;
    private Godot.Collections.Array<MeshInstance3D> _meshInstances = new();


    [Export]
    public Godot.Collections.Array<MeshInstance3D> MeshInstances
    {
        get => _meshInstances;
        private set
        {
            _meshInstances = value;
            UpdateConfigurationWarnings();
        }
    }

    [Export]
    private BuildingCollisionArea CollisionArea
    {
        get => _collisionArea;
        set
        {
            _collisionArea = value;
            UpdateConfigurationWarnings();
        }
    }

    public readonly List<IsPlacementValidCallback> IsPlacementValidCallbacks = new();
    private bool _isPlaced;
    public event Action? PlacedEvent;

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        _validate();
        if (LoadedFromSave)
        {
            OnPlaced();
        }
    }

    public bool IsPlacementValid()
    {
        var buildingIsColliding = _collisionArea.HasOverlappingAreas();
        var isPlacementValid = IsPlacementValidCallbacks.All(e => e.Invoke());
        return !buildingIsColliding && isPlacementValid;
    }

    public void OnPlaced()
    {
        if (_isPlaced) throw new Exception("Building already placed");
        _isPlaced = true;
        PlacedEvent?.Invoke();
    }

    private void _validate()
    {
        if (_collisionArea is null)
        {
            throw new Exception("CollisionArea not assigned!");
        }

        if (_meshInstances.Count == 0)
        {
            throw new Exception("MeshInstances not assigned!");
        }
    }


    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new List<string>();

        if (_collisionArea is null)
        {
            warnings.Add("CollisionArea not assigned!");
        }

        if (_meshInstances.Count == 0)
        {
            warnings.Add("MeshInstances not assigned!");
        }

        return warnings.ToArray();
    }

    public override Dictionary<string, string> Save()
    {
        var saveData = new Dictionary<string, string>
        {
            { "GlobalTransform", GD.VarToStr(GlobalTransform) },
        };
        return saveData;
    }

    public override void Load(Dictionary<string, string> data)
    {
        GlobalTransform = (Transform3D)GD.StrToVar(data["GlobalTransform"]);
    }
}