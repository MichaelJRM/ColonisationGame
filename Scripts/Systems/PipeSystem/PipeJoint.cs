using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseBuilding.Scripts.Systems;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipeJoint : Area3D, IResourceJoint
{
    [Export] public uint MaxConnectionsAllowed { get; protected set; } = 8;
    [Export] protected float MinAngleBetweenLines;
    [Export] public uint MaxLimbs { get; protected set; } = 1;
    [Export] public float MinDistanceBetweenJoints { get; protected set; } = 0f;
    [Export] public float MeshOriginOffset { get; protected set; }

    public readonly List<PipeJoint> ConnectedJoints = new();
    public readonly List<Pipe> ConnectedPipes = new();
    public Pipe? OwnerPipe;
    private uint? _lineId;
    public uint? RenderId { get; private set; }
    public event Action<Pipe> PipeRemovedEvent = null!;
    public event Action<Pipe> PipeAddedEvent = null!;


    public void SetRenderId(uint renderId)
    {
        RenderId = renderId;
    }

    public void SetLineId(uint? lineId)
    {
        _lineId = lineId;
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

    public bool CanConnectToJoint(PipeJoint other)
    {
        if (this == other) return false;
        if (ConnectedJoints.Count >= MaxConnectionsAllowed) return false;

        var directionToJoint = GlobalPosition.DirectionTo(other.GlobalPosition);
        foreach (var joint in CollectionsMarshal.AsSpan(ConnectedJoints))
        {
            var directionToConnectedJoint = GlobalPosition.DirectionTo(joint.GlobalPosition);
            var angleToNextJoint = directionToJoint.SignedAngleTo(directionToConnectedJoint, Basis.Y);
            if (Mathf.Abs(angleToNextJoint) < MinAngleBetweenLines) return false;
        }

        return true;
    }

    public void ConnectToJoint(PipeJoint endJoint, PackedScene pipeScene)
    {
        ConnectedJoints.Add(endJoint);
        endJoint.ConnectedJoints.Add(this);
        var newPipes = _createLimbsBetweenJoints(this, endJoint, pipeScene);
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
        var pipesBetweenJoints =
            ConnectedPipes.Where(pipe => pipe.FrontJoint == joint || pipe.BackJoint == joint).ToArray();
        ConnectedPipes.RemoveAll(pipe => pipesBetweenJoints.Contains(pipe));
        ConnectedJoints.Remove(joint);
        joint.ConnectedPipes.RemoveAll(pipe => pipesBetweenJoints.Contains(pipe));
        joint.ConnectedJoints.Remove(this);
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
}