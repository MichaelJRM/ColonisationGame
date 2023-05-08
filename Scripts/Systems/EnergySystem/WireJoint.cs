using System;
using System.Collections.Generic;
using BaseBuilding.scripts.common;
using BaseBuilding.Scripts.Systems.SaveSystem;
using BaseBuilding.scripts.util.common;
using BaseBuilding.Scripts.Util.objects;
using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem;

public partial class WireJoint : PersistentArea3D<WireJoint.SerializationData>, IResourceJoint
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

    public readonly List<Eid> ConnectedJointsIds = new();
    private readonly List<(MeshInstance3D, WireJoint)> _wireMeshInstances = new();
    protected uint? LineId;
    public Eid Eid { get; protected set; }

    public void SetLineId(uint? lineId)
    {
        LineId = lineId;
        _debugLineIdLabel.Text = $"LineID: {LineId.ToString()}";
    }

    public uint? GetLineId()
    {
        return LineId;
    }

    public void SetId(Eid id)
    {
        Eid = id;
    }

    public bool IsConnectedToLine()
    {
        return LineId != null;
    }

    public bool CanConnect()
    {
        return ConnectedJointsIds.Count < MaxConnectionsAllowed;
    }

    public void ConnectToJoint(WireJoint other)
    {
        if (!other.Eid.IsValid) throw new Exception("Other joint Eid is not valid!");

        if (ConnectedJointsIds.Contains(other.Eid)) return;
        ConnectedJointsIds.Add(other.Eid);
        other.ConnectedJointsIds.Add(Eid);

        CreateWireBetweenJoints(other);
    }

    public void CreateWireBetweenJoints(WireJoint other)
    {
        var mesh = CreateWire(other.WireOrigin.GlobalTransform);
        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = mesh;
        _wireMeshInstances.Add((meshInstance, other));
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


    public override object Save()
    {
        return new SerializationData(
            eid: Eid,
            gt: GD.VarToStr(GlobalTransform),
            li: LineId,
            cj: ConnectedJointsIds.ToArray()
        );
    }

    public override void Load()
    {
        Eid = SaveContent!.Eid;
        GlobalTransform = (Transform3D)GD.StrToVar(SaveContent.Gt);
        LineId = SaveContent.Li;
        ConnectedJointsIds.AddRange(SaveContent.Cj);
    }

    public override bool InstantiateOnLoad() => true;

    public class SerializationData
    {
        public SerializationData(Eid eid, string gt, uint? li, Eid[] cj)
        {
            Eid = eid;
            Gt = gt;
            Li = li;
            Cj = cj;
        }

        /// <summary>
        /// Unique WireJoint Id
        /// </summary>
        public Eid Eid { get; set; }

        /// <summary>
        /// GlobalTransform
        /// </summary>
        public string Gt { get; set; }

        /// <summary>
        /// LineID
        /// </summary>
        public uint? Li { get; set; }


        /// <summary>
        /// ConnectedJointsIds
        /// </summary>
        public Eid[] Cj { get; set; }
    }
}