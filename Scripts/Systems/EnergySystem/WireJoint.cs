using System;
using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.common;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem;

public partial class WireJoint : Area3D, IResourceJoint
{
    [Export] private Label3D _debugLineIdLabel = null!;
    [Export] protected uint MaxConnectionsAllowed = 10;
    [Export] public Marker3D WireOrigin { get; private set; } = null!;

    [Export] protected Vector2[] WireShape =
    {
        new(-0.043947f, 0.03193f),
        new(0.016787f, 0.051663f),
        new(0.054322f, 0f),
        new(0.016787f, -0.051663f),
        new(-0.043947f, -0.03193f),
    };


    protected readonly List<WireJoint> ConnectedJoints = new();
    private readonly List<(MeshInstance3D, WireJoint)> _wireMeshInstances = new();

    private uint? _lineId;

    public void SetLineId(uint? lineId)
    {
        _lineId = lineId;
        _debugLineIdLabel.Text = $"LineID: {_lineId.ToString()}";
    }

    public uint? GetLineId()
    {
        return _lineId;
    }

    public bool IsConnectedToLine()
    {
        return _lineId != null;
    }

    public bool CanConnect()
    {
        return ConnectedJoints.Count < MaxConnectionsAllowed;
    }

    public void ConnectToJoint(WireJoint joint)
    {
        ConnectedJoints.Add(joint);
        joint.ConnectedJoints.Add(this);

        var mesh = CreateWire(joint.WireOrigin.GlobalTransform);
        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = mesh;
        _wireMeshInstances.Add((meshInstance, joint));
        AddChild(meshInstance);
    }

    public ArrayMesh CreateWire(Transform3D target)
    {
        var pipeMeshCurve = new Curve3D();
        pipeMeshCurve.BakeInterval = 5.0f;
        var points = _calculateCatenary(
            startPoint: WireOrigin.Position,
            endPoint: MathUtil.ToLocal(GlobalTransform, target).Origin,
            sag: 30.0f,
            resolution: 5f,
            strength: 0.4f
        );

        foreach (var vector3 in points)
        {
            pipeMeshCurve.AddPoint(vector3);
        }

        var mesh = MeshExtruder.Create(pipeMeshCurve, new[] { WireShape });
        return mesh;
    }

    private static Vector3[] _calculateCatenary(
        Vector3 startPoint,
        Vector3 endPoint,
        float sag,
        float resolution,
        float strength = 1.0f
    )
    {
        var distance = startPoint.DistanceTo(endPoint);
        var points = _distributePointsAlongLine(
            startPoint,
            endPoint,
            Mathf.Max(Mathf.CeilToInt(distance / resolution), 2)
        );
        var catenaryConstant = _calculateCatenaryConstant(startPoint, endPoint, sag);
        var lowestPoint = points[0].Y > points[^1].Y ? points[^1] : points[0];
        var lowestPointPositionInLine = distance / 2.0f;
        var sagFactor = sag + sag * (1f - strength);

        for (var i = 0; i < points.Length; i++)
        {
            var distanceToStart = points[0].DistanceTo(points[i]);
            var distanceToEnd = points[^1].DistanceTo(points[i]);
            var positionInLine = Mathf.Min(distanceToStart, distanceToEnd);
            points[i].Y = (
                catenaryConstant
                * Mathf.Cosh((positionInLine - lowestPointPositionInLine) / catenaryConstant)
                + (Mathf.Abs(lowestPoint.Y - points[i].Y) - sagFactor)
            );
        }

        var firstPointOffset = points[0].Y - startPoint.Y;
        for (var i = 0; i < points.Length; i++)
        {
            points[i].Y -= firstPointOffset;
        }

        return points;
    }

    private static float _calculateCatenaryConstant(Vector3 startPoint, Vector3 endPoint, float sag)
    {
        var l = startPoint.DistanceTo(endPoint);
        var d = l * sag;
        var a = d / (2.0f * Mathf.Log(l / (2.0f * sag) + Mathf.Sqrt(1.0f + l / (2.0f * sag) * (l / (2.0f * sag)))));
        return a;
    }

    private static Vector3[] _distributePointsAlongLine(Vector3 startPos, Vector3 endPos, int n)
    {
        var points = new Vector3[n];
        var distance = startPos.DistanceTo(endPos);
        var direction = startPos.DirectionTo(endPos);
        var pointDistance = distance / (n - 1);

        for (var i = 0; i < n; i++)
        {
            points[i] = startPos + direction * (i * pointDistance);
        }

        return points;
    }
}