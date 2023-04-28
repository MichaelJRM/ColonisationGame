using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipeJoint : Area3D
{
    private const float MinAngleRadBetweenPipes = 0.33f;
    private readonly List<PipeJoint> _connectedPipeJoints = new();
    private readonly List<Pipe> _connectedPipes = new();
    public Pipe? OwnerPipe;
    protected uint? PipeLineId;
    [Export] public int MaxConnections { get; protected set; } = 8;
    [Export] public float MinDistanceBetweenJoints { get; private set; }
    public int? RendererId { get; private set; }
    public event EventHandler<Pipe>? PipeRemovedEvent;
    public event EventHandler<Pipe>? PipeAddedEvent;


    public void SetRenderId(int renderId)
    {
        RendererId = renderId;
        var label3D = GetNode<Label3D>("Label3D2");
        label3D.Text = $"RendererId: {RendererId.ToString()}";
    }

    public bool IsConnectedToOtherJoints()
    {
        return _connectedPipeJoints.Count > 0;
    }

    public uint? GetPipeLineId()
    {
        return PipeLineId;
    }

    public void SetPipeLineId(uint pipeLineId)
    {
        PipeLineId = pipeLineId;
        var label = GetNode<Label3D>("Label3D");
        label.Text = PipeLineId.ToString();
    }

    public bool IsConnectedToPipeLine()
    {
        return PipeLineId != null;
    }

    public void ConnectToJoint(PipeJoint endPipeJoint, PackedScene pipeScene)
    {
        _connectedPipeJoints.Add(endPipeJoint);
        endPipeJoint._connectedPipeJoints.Add(this);
        var newPipes = _placePipesBetweenJoints(this, endPipeJoint, pipeScene);
        foreach (var newPipe in newPipes)
        {
            newPipe.SetBackPipeJoint(this);
            newPipe.SetFrontPipeJoint(endPipeJoint);
            PipeAddedEvent?.Invoke(this, newPipe);
        }

        _connectedPipes.AddRange(newPipes);
        endPipeJoint._connectedPipes.AddRange(newPipes);
    }

    public void DisconnectFromJoint(PipeJoint pipeJoint)
    {
        var pipesBetweenJoints = _connectedPipes
            .Where(pipe => pipe.FrontPipeJoint == pipeJoint || pipe.BackPipeJoint == pipeJoint).ToList();
        _connectedPipes.RemoveAll(pipe => pipesBetweenJoints.Contains(pipe));
        _connectedPipeJoints.Remove(pipeJoint);
        pipeJoint._connectedPipes.RemoveAll(pipe => pipesBetweenJoints.Contains(pipe));
        pipeJoint._connectedPipeJoints.Remove(this);
        pipesBetweenJoints.ForEach(pipe => PipeRemovedEvent?.Invoke(this, pipe));
    }

    public bool CanConnectToJoint(PipeJoint other)
    {
        if (this == other) return false;
        if (_connectedPipeJoints.Count >= MaxConnections) return false;

        var directionToJoint = GlobalPosition.DirectionTo(other.GlobalPosition);
        foreach (var joint in _connectedPipeJoints)
        {
            var directionToConnectedJoint = GlobalPosition.DirectionTo(joint.GlobalPosition);
            var angleToNextJoint = directionToJoint.SignedAngleTo(directionToConnectedJoint, Basis.Y);
            if (Mathf.Abs(angleToNextJoint) < MinAngleRadBetweenPipes) return false;
        }

        return true;
    }

    private List<Pipe> _placePipesBetweenJoints(PipeJoint from, PipeJoint to, PackedScene pipeScene)
    {
        var toPosition = to.IsInsideTree() ? to.GlobalPosition : to.Position;
        var firstPipe = pipeScene.Instantiate<Pipe>();
        firstPipe.QueueFree();
        var pipeLength = firstPipe.MaxLength;
        var distance = from.GlobalPosition.DistanceTo(toPosition);
        var count = Mathf.CeilToInt(distance / pipeLength);
        var pipes = new List<Pipe>(count);
        if (count == 0) return pipes;
        var transform =
            from.GlobalTransform.LookingAt(toPosition, from.Basis.Y);

        for (var i = 0; i < count; i++)
        {
            var pipe = pipeScene.Instantiate<Pipe>();
            var collisionShape = pipe.GetNode<CollisionShape3D>("CollisionShape3D");
            var boxShape = new BoxShape3D();
            boxShape.Size = new Vector3(pipe.Width, pipe.Height, pipeLength);
            collisionShape.Shape = boxShape;
            pipe.ActualLength = pipeLength;
            pipe.GlobalTransform =
                transform.TranslatedLocal(new Vector3(0.0f, 0.0f, -(pipeLength * i + pipeLength / 2)));
            pipes.Add(pipe);
        }

        // Adjust the size of the last pipe to fit the distance to the joint.
        var lastPipe = pipes.Last();
        var lastPipeCollisionShape = (lastPipe.GetNode<CollisionShape3D>("CollisionShape3D").Shape as BoxShape3D)!;
        var isPipeOriginBehindTarget =
            lastPipe.Position.DirectionTo(toPosition).Dot(-lastPipe.Basis.Z) >
            0;
        var lastPopeDistanceToJoint = lastPipe.Position.DistanceTo(toPosition);
        var overflow = isPipeOriginBehindTarget
            ? pipeLength / 2 - lastPopeDistanceToJoint
            : pipeLength / 2 + lastPopeDistanceToJoint;
        lastPipeCollisionShape.Size -= new Vector3(0.0f, 0.0f, overflow);
        lastPipe.Transform = lastPipe.Transform.TranslatedLocal(new Vector3(0.0f, 0.0f, overflow / 2));
        lastPipe.ActualLength = lastPipeCollisionShape.Size.Z;
        return pipes;
    }
}