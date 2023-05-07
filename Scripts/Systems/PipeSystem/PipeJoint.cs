using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseBuilding.Scripts.Systems;
using BaseBuilding.Scripts.Systems.SaveSystem;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipeJoint : PersistentArea3D<PipeJoint.SerializationData>, IResourceJoint
{
    [Export] public uint MaxConnectionsAllowed { get; protected set; } = 8;
    [Export] protected float MinAngleBetweenLines;
    [Export] public float MinDistanceBetweenJoints { get; protected set; } = 0f;
    [Export] public float MeshOriginOffset { get; protected set; }
    [Export] private PackedScene _pipeScene = null!;

    public Pipe? OwnerPipe;
    public event Action<Pipe> PipeRemovedEvent = null!;
    public event Action<Pipe> PipeAddedEvent = null!;
    public readonly List<ulong> ConnectedJointsIds = new();
    public readonly List<Pipe> ConnectedPipes = new();
    private uint? _lineId;
    private uint? _renderId;
    public ulong Id { get; private set; }

    public void SetRenderId(uint renderId)
    {
        _renderId = renderId;
    }

    public void SetLineId(uint? lineId)
    {
        _lineId = lineId;
    }

    public void SetId(ulong id)
    {
        Id = id;
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
        return ConnectedJointsIds.Count < MaxConnectionsAllowed;
    }

    public bool CanConnectToJoint(PipeJoint other)
    {
        if (this == other) return false;
        if (ConnectedJointsIds.Count >= MaxConnectionsAllowed) return false;

        var directionToJoint = GlobalPosition.DirectionTo(other.GlobalPosition);
        foreach (var jointRid in CollectionsMarshal.AsSpan(ConnectedJointsIds))
        {
            var joint = PipeSystem.Instance.GetPipeJoint(jointRid);
            var directionToConnectedJoint = GlobalPosition.DirectionTo(joint.GlobalPosition);
            var angleToNextJoint = directionToJoint.SignedAngleTo(directionToConnectedJoint, Basis.Y);
            if (Mathf.Abs(angleToNextJoint) < MinAngleBetweenLines) return false;
        }

        return true;
    }

    public void ConnectToJoint(PipeJoint endJoint)
    {
        if (ConnectedJointsIds.Contains(endJoint.Id)) return;
        ConnectedJointsIds.Add(endJoint.Id);
        endJoint.ConnectedJointsIds.Add(Id);

        var newPipes = _createLimbsBetweenJoints(this, endJoint, _pipeScene);
        foreach (var pipe in newPipes)
        {
            pipe.SetBackJoint(this);
            pipe.SetFrontJoint(endJoint);
            PipeAddedEvent.Invoke(pipe);
        }

        ConnectedPipes.AddRange(newPipes);
        endJoint.ConnectedPipes.AddRange(newPipes);
    }


    public void DisconnectFromJoint(PipeJoint joint)
    {
        var pipesBetweenJoints = ConnectedPipes
            .Where(pipe => pipe.FrontJointId == joint.Id || pipe.BackJointId == joint.Id)
            .ToArray();
        ConnectedPipes.RemoveAll(pipe => pipesBetweenJoints.Contains(pipe));
        ConnectedJointsIds.Remove(joint.Id);
        joint.ConnectedPipes.RemoveAll(pipe => pipesBetweenJoints.Contains(pipe));
        joint.ConnectedJointsIds.Remove(Id);
        foreach (var pipe in pipesBetweenJoints)
        {
            PipeRemovedEvent.Invoke(pipe);
        }
    }

    protected Pipe[] _createLimbsBetweenJoints(
        PipeJoint startJoint,
        PipeJoint endJoint,
        PackedScene limbScene
    )
    {
        var toPosition = endJoint.IsInsideTree() ? endJoint.GlobalPosition : endJoint.Position;
        var firstPipe = limbScene.Instantiate<Pipe>();
        var pipeSize = firstPipe.CreateMesh(firstPipe.Length).GetAabb().Size;
        firstPipe.QueueFree();
        var distance = startJoint.GlobalPosition.DistanceTo(toPosition);
        var count = Mathf.CeilToInt(distance / pipeSize.Z);
        var pipes = new List<Pipe>(count);
        if (count == 0) return pipes.ToArray();
        var transform = startJoint.GlobalTransform.LookingAt(toPosition, startJoint.Basis.Y);

        for (var i = 0; i < count; i++)
        {
            var pipe = limbScene.Instantiate<Pipe>();
            var collisionShape = new CollisionShape3D();
            var cylinderShape = new CylinderShape3D();
            cylinderShape.Height = pipeSize.Z;
            cylinderShape.Radius = pipeSize.X * 0.5f;
            collisionShape.Shape = cylinderShape;
            pipe.CollisionShape = collisionShape;
            pipe.AddChild(collisionShape);
            // The collision shape is created with the Z axis pointing up, but we want it to point forward.
            collisionShape.RotateObjectLocal(Vector3.Right, Mathf.Tau / 4);
            pipe.GlobalTransform =
                transform.TranslatedLocal(new Vector3(0.0f, 0.0f, -(pipeSize.Z * i + pipeSize.Z * 0.5f)));
            pipes.Add(pipe);
        }

        // Adjust the size of the last pipe to fit the distance to the joint.
        var lastPipe = pipes.Last();
        var lastPipeCollisionShape = (CylinderShape3D)lastPipe.CollisionShape.Shape!;
        var isPipeOriginBehindTarget =
            lastPipe.Position.DirectionTo(toPosition).Dot(-lastPipe.Basis.Z) >
            0;
        var lastPopeDistanceToJoint = lastPipe.Position.DistanceTo(toPosition);
        var overflow = isPipeOriginBehindTarget
            ? pipeSize.Z * 0.5f - lastPopeDistanceToJoint
            : pipeSize.Z * 0.5f + lastPopeDistanceToJoint;
        lastPipeCollisionShape.Height -= overflow;
        lastPipe.Transform = lastPipe.Transform.TranslatedLocal(new Vector3(0.0f, 0.0f, overflow * 0.5f));
        return pipes.ToArray();
    }

    public override object Save()
    {
        return new SerializationData(
            id: Id,
            gt: GD.VarToStr(GlobalTransform),
            li: _lineId,
            cj: ConnectedJointsIds.ToArray()
        );
    }

    public override void Load()
    {
        Id = SaveContent.Id;
        GlobalTransform = (Transform3D)GD.StrToVar(SaveContent.Gt);
        _lineId = SaveContent.Li;
    }


    public class SerializationData
    {
        public SerializationData(ulong id, string gt, uint? li, ulong[] cj)
        {
            Id = id;
            Gt = gt;
            Li = li;
            Cj = cj;
        }

        /// <summary>
        /// Unique PipeJoint Id
        /// </summary>
        public ulong Id { get; set; }

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
        public ulong[] Cj { get; set; }
    }
}