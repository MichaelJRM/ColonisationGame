using System.Linq;
using BaseBuilding.scripts.singletons;
using BaseBuilding.Scripts.Util;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.WirePlacement;

public partial class WireDetector : Area3D
{
    private Global _global = null!;

    public override void _Ready()
    {
        SetCollisionLayerValue(1, false);
        SetCollisionMaskValue(1, false);
        SetCollisionMaskValue(CollisionIndexes.Building, true);
        SetCollisionMaskValue(CollisionIndexes.Pipe, true);
        SetCollisionMaskValue(CollisionIndexes.Wire, true);
        var collisionShape = new CollisionShape3D();
        var shape = new SphereShape3D();
        shape.Radius = 0.2f;
        collisionShape.Shape = shape;
        AddChild(collisionShape);
        _global = GetNode<Global>("/root/Global");
        GlobalPosition = _global.MousePositionInWorld;
    }

    public override void _Process(double delta)
    {
        GlobalPosition = _global.MousePositionInWorld;
    }

    public WireJoint? GetClosestDetectedWireJoint()
    {
        var detectedJoints = GetOverlappingAreas().OfType<WireJoint>().ToArray();
        return NodeUtil.FindClosestNode(GlobalPosition, detectedJoints);
    }
}