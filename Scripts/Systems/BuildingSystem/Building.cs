using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public delegate bool IsPlacementValidCallback();

public partial class Building : Node3D
{
    [Export] public Area3D CollisionArea = null!;
    [Export] public MeshInstance3D[] MeshInstance3Ds { get; private set; } = Array.Empty<MeshInstance3D>();

    public readonly List<IsPlacementValidCallback> IsPlacementValidCallbacks = new();
    private bool _isPlaced;
    public event Action? PlacedEvent;


    public bool IsPlacementValid()
    {
        return !CollisionArea.HasOverlappingAreas() && IsPlacementValidCallbacks.All(e => e.Invoke());
    }

    public void OnPlaced()
    {
        if (_isPlaced) throw new Exception("Building already placed");
        CollisionArea.Monitoring = false;
        _isPlaced = true;
        PlacedEvent?.Invoke();
    }
}