using System.Linq;
using BaseBuilding.scripts.singletons;
using BaseBuilding.Scripts.Util;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.WirePlacement;

public partial class WireDetector : Area3D
{
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
        GlobalPosition = Global.Instance.GetMousePositionInWorld();
    }

    public override void _Process(double delta)
    {
        GlobalPosition = Global.Instance.GetMousePositionInWorld();
    }

    public WireJoint? GetClosestDetectedWireJoint()
    {
        var detectedJoints = GetOverlappingAreas().OfType<WireJoint>().ToArray();
        return NodeUtil.FindClosestNode(GlobalPosition, detectedJoints);
    }
}