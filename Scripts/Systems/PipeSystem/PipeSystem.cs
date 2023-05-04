using System;
using BaseBuilding.Scripts.Systems;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public sealed partial class PipeSystem : Node3D
{
    [Export] private PackedScene _pipeJointScene = null!;
    [Export] private PackedScene _pipeScene = null!;
    [Export] private PackedScene _pipeTemporaryJointScene = null!;
    [Export] private PackedScene _temporaryPipeScene = null!;
    [Export] private Mesh _pipeJointMesh = null!;

    private readonly ResourceLineManager<PipeJoint, PipeConnector> _pipeLineManager = new();
    private readonly ResourceLineRenderer _pipeLineRenderer = new();
    private bool _isEnabled;
    private Scripts.Systems.PipeSystem.PipePlacement.PipePlacerSystem? _pipePlacer;

    private PipeSystem()
    {
    }

    public static PipeSystem Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
    }

    public void RegisterPipeJoint(PipeJoint joint)
    {
        joint.PipeAddedEvent += OnPipeAdded;
        joint.PipeRemovedEvent += OnPipeRemoved;
        joint.TreeExiting += OnExitedTree;

        void OnExitedTree()
        {
            joint.PipeAddedEvent -= OnPipeAdded;
            joint.PipeRemovedEvent -= OnPipeRemoved;
            joint.TreeExiting -= OnExitedTree;
        }
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
        _disposePipePlacer();
    }

    private void _initPipePlacer()
    {
        _pipePlacer = new Scripts.Systems.PipeSystem.PipePlacement.PipePlacerSystem(
            _completePipePlacement,
            _pipeTemporaryJointScene,
            _temporaryPipeScene
        );
        AddChild(_pipePlacer);
    }

    private void _disposePipePlacer()
    {
        _pipePlacer?.Disable();
        _pipePlacer = null!;
    }

    private void _resetPipePlacer()
    {
        _disposePipePlacer();
        _initPipePlacer();
    }

    private void _completePipePlacement(PipeJoint[] pipeJoints)
    {
        if (!_pipePlacer!.IsPlacementValid) return;
        var processedJoints = new PipeJoint[pipeJoints.Length].AsSpan();

        for (var i = 0; i < pipeJoints.Length; i++)
        {
            processedJoints[i] = TransformTempIntoPermIfNecessary(pipeJoints[i]);
        }

        for (var i = 0; i < processedJoints.Length - 1; i++)
        {
            ConnectJoints(processedJoints[i], processedJoints[i + 1]);
        }

        _resetPipePlacer();
        _pipePlacer!.Enable(processedJoints[^1]);
        return;

        // Helper functions //
        PipeJoint TransformTempIntoPermIfNecessary(PipeJoint pipeJoint)
        {
            return pipeJoint is TemporaryPipeJoint temporaryPipeJoint
                ? CreatePermanentPipeJointFromTemporary(temporaryPipeJoint)
                : pipeJoint;

            PipeJoint CreatePermanentPipeJointFromTemporary(TemporaryPipeJoint tempPipeJoint)
            {
                var permJoint = tempPipeJoint.OwnerPipe == null
                    ? CreatePermanentPipeJointAtPosition(tempPipeJoint.GlobalPosition)
                    : CreatePermanentPipeJointOnPipe(tempPipeJoint.OwnerPipe,
                        tempPipeJoint.GlobalPosition);
                tempPipeJoint.QueueFree();
                return permJoint;

                PipeJoint CreatePermanentPipeJointAtPosition(Vector3 globalPosition)
                {
                    var instance = _pipeJointScene.Instantiate<PipeJoint>();
                    instance.Position = globalPosition;
                    CommitPipeJoint(instance);
                    return instance;
                }

                PipeJoint CreatePermanentPipeJointOnPipe(Pipe pipe, Vector3 globalPosition)
                {
                    var instance = _pipeJointScene.Instantiate<PipeJoint>();
                    instance.OwnerPipe = pipe;
                    instance.Position = MathUtil.GetParallelPosition(pipe.GlobalTransform, globalPosition);
                    CommitPipeJoint(instance);
                    pipe.FrontJoint.DisconnectFromJoint(pipe.BackJoint);
                    pipe.BackJoint.ConnectToJoint(instance, _pipeScene);
                    pipe.FrontJoint.ConnectToJoint(instance, _pipeScene);
                    var pipeLineId = (uint)(pipe.BackJoint.GetLineId() ?? pipe.FrontJoint.GetLineId())!;
                    _pipeLineManager.AddJoint(pipeLineId, instance);
                    return instance;
                }

                void CommitPipeJoint(PipeJoint joint)
                {
                    joint.PipeAddedEvent += OnPipeAdded;
                    joint.PipeRemovedEvent += OnPipeRemoved;
                    joint.TreeExited += OnJointRemoved;
                    AddChild(joint);
                    joint.SetRenderId(
                        _pipeLineRenderer.AddJoint(
                            GetWorld3D(),
                            _pipeJointMesh,
                            joint.GlobalTransform.TranslatedLocal(new Vector3(0.0f, joint.MeshOriginOffset, 0.0f))
                        )
                    );


                    void OnJointRemoved()
                    {
                        joint.PipeAddedEvent -= OnPipeAdded;
                        joint.PipeRemovedEvent -= OnPipeRemoved;
                        joint.TreeExited -= OnJointRemoved;
                    }
                }
            }
        }

        void ConnectJoints(PipeJoint startPipeJoint, PipeJoint endPipeJoint)
        {
            startPipeJoint.ConnectToJoint(endPipeJoint, _pipeScene);
            _pipeLineManager.Connect(startPipeJoint, endPipeJoint);
        }
    }

    void OnPipeAdded(Pipe pipe)
    {
        AddChild(pipe);
        pipe.SetRenderId(
            _pipeLineRenderer.AddLimb(
                GetWorld3D(),
                pipe.CreateMesh(true),
                pipe.GlobalTransform
            )
        );
    }

    void OnPipeRemoved(Pipe pipe)
    {
        _pipeLineRenderer.RemoveLimb(
            (uint)pipe.RenderId!,
            pipe.GlobalTransform
        );
        pipe.QueueFree();
    }


    public override void _ExitTree()
    {
        _pipeLineRenderer.Clean();
    }
}