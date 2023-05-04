﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseBuilding.scripts.common;
using BaseBuilding.scripts.systems.PipeSystem;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipePlacement;

public partial class PipePlacerSystem : Node
{
    private readonly List<TemporaryPipeJoint> _intermediateJoints = new();
    private readonly List<TemporaryPipeJoint> _intermediateJointsPoll = new();
    private readonly Action<PipeJoint[]> _onPlace;
    private readonly PackedScene _temporaryJointScene;
    private readonly PackedScene _temporaryPipeScene;
    private bool _areJointsValid;
    private PipeJoint? _startJoint;
    private PipeJoint? _endJoint;
    private PipeDetector _pipeDetector = new();
    private PipeGenerator _temporaryPipeGenerator = null!;
    private TickComponent _inputTick = new();
    private Status _status = Status.Disabled;


    public PipePlacerSystem(
        Action<PipeJoint[]> onPlace,
        PackedScene temporaryJointScene,
        PackedScene temporaryPipeScene
    )
    {
        _onPlace = onPlace;
        _temporaryJointScene = temporaryJointScene;
        _temporaryPipeScene = temporaryPipeScene;
    }

    public bool IsPlacementValid { get; private set; }

    public override void _Ready()
    {
        SetProcessUnhandledInput(false);
        AddChild(_pipeDetector);
        _temporaryPipeGenerator = new PipeGenerator(_temporaryPipeScene, () => IsPlacementValid);
        AddChild(_temporaryPipeGenerator);
        AddChild(_inputTick);
        _inputTick.Pause();
        _inputTick.SetTickRateInFps(60);
        _inputTick.SetOnTick(_calculate);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("build_manager_place_item")) return;

        switch (_status)
        {
            case Status.Disabled:
                break;
            case Status.PlacingStartJoint:
                _status = Status.PlacingEndJoint;
                break;
            case Status.PlacingEndJoint:
                _calculateIfPlacementIsValid();
                if (IsPlacementValid)
                {
                    _onPlace(_getAllJoints());
                }

                break;
        }
    }

    public void Enable(PipeJoint? startJoint = null)
    {
        _inputTick.Resume();
        SetProcessUnhandledInput(true);
        _startJoint = startJoint;
        _endJoint = null;
        _status = _startJoint == null ? Status.PlacingStartJoint : Status.PlacingEndJoint;
    }

    public void Disable()
    {
        QueueFree();
    }

    private void _calculate()
    {
        switch (_status)
        {
            case Status.Disabled:
                return;
            case Status.PlacingStartJoint:
                _calculateStartJoint();
                return;
            case Status.PlacingEndJoint:
                var updated = _calculateEndJoint();
                if (!updated) return;

                _calculateIfPlacementIsValid();
                _calculateIntermediateJoints();
                if (_startJoint != null && _endJoint != null)
                    _temporaryPipeGenerator.Update(_startJoint!.GlobalTransform, _endJoint!.GlobalTransform);

                return;
        }
    }


    private void _calculateStartJoint()
    {
        var closestDetectedPipeJoint = _pipeDetector.GetClosestDetectedPipeJoint();
        if (closestDetectedPipeJoint != null)
        {
            if (_startJoint is TemporaryPipeJoint) _startJoint.QueueFree();
            _startJoint = closestDetectedPipeJoint;
            return;
        }

        var closestDetectedPipe = _pipeDetector.GetClosestDetectedPipe();
        if (closestDetectedPipe != null)
        {
            if (_startJoint is TemporaryPipeJoint) _startJoint.QueueFree();
            _startJoint = _createTemporaryJointOnPipe(closestDetectedPipe, _pipeDetector.GlobalPosition);
            return;
        }

        if (_startJoint is TemporaryPipeJoint temporaryJoint)
        {
            temporaryJoint.OwnerPipe = null;
            temporaryJoint.GlobalPosition = _pipeDetector.GlobalPosition;
        }
        else
        {
            _startJoint = _createTemporaryJointAtPosition(_pipeDetector.GlobalPosition);
        }
    }

    private bool _calculateEndJoint()
    {
        if (_endJoint?.GlobalPosition == _pipeDetector.GlobalPosition) return false;

        var closestDetectedPipeJoint = _pipeDetector.GetClosestDetectedPipeJoint();
        if (closestDetectedPipeJoint != null)
        {
            if (closestDetectedPipeJoint == _startJoint)
            {
                _temporaryPipeGenerator.Clear();
                return true;
            }

            if (_endJoint is TemporaryPipeJoint) _endJoint.QueueFree();
            _endJoint = closestDetectedPipeJoint;
            return true;
        }


        var closestDetectedPipe = _pipeDetector.GetClosestDetectedPipe();
        if (closestDetectedPipe != null)
        {
            var newJoint = _createTemporaryJointOnPipe(closestDetectedPipe, _pipeDetector.GlobalPosition);
            if (_endJoint is TemporaryPipeJoint) _endJoint.QueueFree();
            _endJoint = newJoint;
            return false;
        }

        if (_endJoint is TemporaryPipeJoint temporaryJoint)
        {
            temporaryJoint.OwnerPipe = null;
            temporaryJoint.GlobalPosition = _pipeDetector.GlobalPosition;
        }
        else
        {
            _endJoint = _createTemporaryJointAtPosition(_pipeDetector.GlobalPosition);
        }

        return true;
    }

    private void _calculateIfPlacementIsValid()
    {
        // var overlappingAreas = _pipeDetector.GetOverlappingAreas();
        // var isColliding = overlappingAreas.Any(e => e is not PipeJoint && e is not Pipe);
        // if (isColliding)
        // {
        //     IsPlacementValid = false;
        //     return;
        // }

        var isPipePlacementValid = _temporaryPipeGenerator.IsPlacementValid;
        if (!isPipePlacementValid)
        {
            IsPlacementValid = false;
            return;
        }

        var allJoints = new List<PipeJoint>(_intermediateJoints.Count + 2);
        if (_startJoint != null) allJoints.Add(_startJoint!);
        allJoints.AddRange(_intermediateJoints);
        if (_endJoint != null) allJoints.Add(_endJoint!);

        _areJointsValid = true;
        for (var i = 0; i < allJoints.Count - 1; i++)
            if (!allJoints[i].CanConnectToJoint(allJoints[i + 1]))
            {
                _areJointsValid = false;
                break;
            }

        IsPlacementValid = _areJointsValid;
    }

    private void _calculateIntermediateJoints()
    {
        foreach (var joint in CollectionsMarshal.AsSpan(_intermediateJointsPoll))
        {
            joint.OwnerPipe = null;
            joint.GlobalPosition = new Vector3(0.0f, -1000.0f, 0.0f);
        }

        if (!_areJointsValid) return;
        _intermediateJoints.Clear();

        var snap = new Vector3(0.01f, 0.01f, 0.01f);
        var intersectingPipes = _temporaryPipeGenerator.GetIntersectingPipes();
        if (intersectingPipes.Count > 0)
        {
            var removeFirst = intersectingPipes.First().Item2.Snapped(snap) ==
                              _startJoint!.GlobalPosition.Snapped(snap);
            if (intersectingPipes.Count > 1)
            {
                var removeLast = intersectingPipes.Last().Item2.Snapped(snap) ==
                                 _endJoint!.GlobalPosition.Snapped(snap);
                if (removeLast) intersectingPipes.RemoveAt(intersectingPipes.Count - 1);
            }

            if (removeFirst) intersectingPipes.RemoveAt(0);
        }

        var pollJointIndex = _intermediateJointsPoll.Count - 1;
        foreach (var (pipe, intersectionGlobalPosition) in CollectionsMarshal.AsSpan(intersectingPipes))
        {
            if (!pipe.CanCreateJointAtPosition(intersectionGlobalPosition)) continue;

            if (pollJointIndex >= 0)
            {
                var pollJoint = _intermediateJointsPoll[pollJointIndex];
                pollJointIndex--;
                pollJoint.OwnerPipe = pipe;
                pollJoint.GlobalPosition =
                    MathUtil.GetParallelPosition(pipe.GlobalTransform, intersectionGlobalPosition);
                _intermediateJoints.Add(pollJoint);
            }
            else
            {
                var joint = _createTemporaryJointOnPipe(pipe, intersectionGlobalPosition);
                _intermediateJointsPoll.Add(joint);
                _intermediateJoints.Add(joint);
            }
        }
    }

    private TemporaryPipeJoint _createTemporaryJointOnPipe(Pipe pipe, Vector3 globalPosition)
    {
        var pipeJoint = _temporaryJointScene.Instantiate<TemporaryPipeJoint>();
        pipeJoint.Position = MathUtil.GetParallelPosition(pipe.GlobalTransform, globalPosition);
        pipeJoint.OwnerPipe = pipe;
        AddChild(pipeJoint);
        return pipeJoint;
    }

    private TemporaryPipeJoint _createTemporaryJointAtPosition(Vector3 globalPosition)
    {
        var pipeJoint = _temporaryJointScene.Instantiate<TemporaryPipeJoint>();
        pipeJoint.Position = globalPosition;
        AddChild(pipeJoint);
        return pipeJoint;
    }

    private PipeJoint[] _getAllJoints()
    {
        _calculateIntermediateJoints();
        var allJoints = new List<PipeJoint>(_intermediateJoints.Count + 2);
        allJoints.Add(_startJoint!);
        allJoints.AddRange(_intermediateJoints);
        allJoints.Add(_endJoint!);
        return allJoints.OrderBy(e => e.GlobalPosition.DistanceSquaredTo(_startJoint!.GlobalPosition)).ToArray();
    }

    private enum Status
    {
        Disabled,
        PlacingStartJoint,
        PlacingEndJoint
    }
}