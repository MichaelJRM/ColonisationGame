using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public delegate bool IsPlacementValidCallback();

public partial class Building : Node3D
{
    public readonly List<IsPlacementValidCallback> PlacementValidCallbacks = new();
    private bool _isPlaced;
    [Export] public Area3D CollisionArea = null!;
    public event Action? PlacedEvent;


    public virtual bool IsPlacementValid()
    {
        return CollisionArea.GetOverlappingAreas().Count == 0
               && PlacementValidCallbacks.All(e => e.Invoke());
    }

    public virtual void OnPlaced()
    {
        if (_isPlaced) throw new Exception("Building already placed");
        CollisionArea.Monitoring = false;
        _isPlaced = true;
        PlacedEvent?.Invoke();
    }

    public MeshInstance3D[] GetAllMeshInstances()
    {
        var meshNode = GetNodeOrNull<Node3D>($"{Name}");
        var children = meshNode != null ? meshNode.GetChildren() : GetChildren();
        var meshInstances = new List<MeshInstance3D>(children.Count);
        foreach (var child in children)
            if (child is MeshInstance3D meshInstance3D)
                meshInstances.Add(meshInstance3D);
        return meshInstances.ToArray();
    }
}