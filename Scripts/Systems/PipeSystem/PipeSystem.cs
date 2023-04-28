using System.Linq;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipeSystem : Node3D
{
    private readonly PipeLineManager _pipeLineManager = new();
    private bool _isEnabled;
    [Export] private PackedScene _pipeDetectorScene = null!;
    [Export] private Mesh _pipeJointMesh = null!;
    [Export] private PackedScene _pipeJointScene = null!;
    private PipeLineRenderer _pipeLineRenderer = new();
    private PipePlacer? _pipePlacer;
    [Export] private PackedScene _pipeScene = null!;
    [Export] private PackedScene _pipeTemporaryJointScene = null!;
    [Export] private PackedScene _temporaryPipeScene = null!;


    public void RegisterPipeConnector(PipeConnector pipeConnector)
    {
        pipeConnector.PipeRemovedEvent += _onPipeRemoved;
        pipeConnector.PipeAddedEvent += _onPipeAdded;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pipe_system_toggle")) _toggle();
    }

    private void _toggle()
    {
        if (_isEnabled)
            _disable();
        else
            _enable();
    }

    private void _enable()
    {
        _isEnabled = true;
        _initPipePlacer();
        _pipePlacer!.Enable();
    }

    private void _disable()
    {
        _isEnabled = false;
        _cleanPipePlacer();
    }

    private void _initPipePlacer()
    {
        _pipePlacer = new PipePlacer(
            this,
            _completePipePlacement,
            _pipeDetectorScene,
            _pipeTemporaryJointScene,
            _temporaryPipeScene
        );
        AddChild(_pipePlacer);
    }

    private void _cleanPipePlacer()
    {
        _pipePlacer?.Disable();
        _pipePlacer = null!;
    }

    private void _resetPipePlacer()
    {
        _cleanPipePlacer();
        _initPipePlacer();
    }

    private void _completePipePlacement(PipeJoint[] pipeJoints)
    {
        if (!_pipePlacer!.IsPlacementValid) return;
        var processedPipeJoints = pipeJoints.Select(
            pipeJoint => pipeJoint is TemporaryPipeJoint temporaryPipeJoint
                ? _createPermanentPipeJointFromTemporary(temporaryPipeJoint)
                : pipeJoint
        ).ToArray();
        for (var i = 0; i < processedPipeJoints.Length - 1; i++)
            _connectJoints(processedPipeJoints[i], processedPipeJoints[i + 1]);
        _resetPipePlacer();
        _pipePlacer!.Enable(processedPipeJoints.Last());
    }

    private void _connectJoints(PipeJoint startPipeJoint, PipeJoint endPipeJoint)
    {
        startPipeJoint.ConnectToJoint(endPipeJoint, _pipeScene);

        if (startPipeJoint.IsConnectedToPipeLine() && endPipeJoint.IsConnectedToPipeLine())
        {
            if (startPipeJoint.GetPipeLineId() != endPipeJoint.GetPipeLineId())
                _pipeLineManager.MergePipeLines(
                    (uint)startPipeJoint.GetPipeLineId()!,
                    (uint)endPipeJoint.GetPipeLineId()!
                );
        }
        else if (!startPipeJoint.IsConnectedToPipeLine() && !endPipeJoint.IsConnectedToPipeLine())
        {
            var pipeLineId = _pipeLineManager.CreatePipeLine();
            _pipeLineManager.AddPipeJoint(startPipeJoint, pipeLineId);
            _pipeLineManager.AddPipeJoint(endPipeJoint, pipeLineId);
        }
        else
        {
            if (startPipeJoint.IsConnectedToPipeLine())
            {
                var pipeLineId = (uint)startPipeJoint.GetPipeLineId()!;
                _pipeLineManager.AddPipeJoint(endPipeJoint, pipeLineId);
            }

            else
            {
                var pipeLineId = (uint)endPipeJoint.GetPipeLineId()!;
                _pipeLineManager.AddPipeJoint(startPipeJoint, pipeLineId);
            }
        }
    }


    private PipeJoint _createPermanentPipeJointFromTemporary(TemporaryPipeJoint temporaryPipeJoint)
    {
        var pipeJoint = temporaryPipeJoint.OwnerPipe == null
            ? _createPermanentPipeJointAtPosition(temporaryPipeJoint.GlobalPosition)
            : _createPermanentPipeJointOnPipe(temporaryPipeJoint.OwnerPipe, temporaryPipeJoint.GlobalPosition);
        temporaryPipeJoint.QueueFree();
        return pipeJoint;
    }

    private PipeJoint _createPermanentPipeJointAtPosition(Vector3 globalPosition)
    {
        var pipeJoint = _pipeJointScene.Instantiate<PipeJoint>();
        pipeJoint.Position = globalPosition;
        _commitPipeJoint(pipeJoint);
        return pipeJoint;
    }

    private PipeJoint _createPermanentPipeJointOnPipe(Pipe pipe, Vector3 globalPosition)
    {
        var pipeJoint = _pipeJointScene.Instantiate<PipeJoint>();
        pipeJoint.OwnerPipe = pipe;
        pipeJoint.Position = MathUtil.GetParallelPosition(pipe.GlobalTransform, globalPosition);
        _commitPipeJoint(pipeJoint);
        pipe.FrontPipeJoint.DisconnectFromJoint(pipe.BackPipeJoint);
        pipe.BackPipeJoint.ConnectToJoint(pipeJoint, _pipeScene);
        pipe.FrontPipeJoint.ConnectToJoint(pipeJoint, _pipeScene);
        var pipeLineId = (uint)(pipe.BackPipeJoint.GetPipeLineId() ?? pipe.FrontPipeJoint.GetPipeLineId())!;
        _pipeLineManager.AddPipeJoint(pipeJoint, pipeLineId);
        return pipeJoint;
    }

    private void _commitPipeJoint(PipeJoint pipeJoint)
    {
        pipeJoint.PipeAddedEvent += _onPipeAdded;
        pipeJoint.PipeRemovedEvent += _onPipeRemoved;
        AddChild(pipeJoint);
        pipeJoint.SetRenderId(
            _pipeLineRenderer.AddJoint(
                GetWorld3D(),
                _pipeJointMesh,
                pipeJoint.GlobalTransform
            )
        );
    }

    private void _onPipeAdded(object? sender, Pipe pipe)
    {
        AddChild(pipe);
        pipe.SetRenderId(_pipeLineRenderer.AddPipe(GetWorld3D(), pipe.CreateMesh(), pipe.GlobalTransform));
    }

    private void _onPipeRemoved(object? sender, Pipe pipe)
    {
        _pipeLineRenderer.RemovePipe((int)pipe.RendererId!, pipe.GlobalTransform);
        pipe.QueueFree();
    }

    public override void _ExitTree()
    {
        _pipeLineRenderer.Clean();
    }
}