using System.Collections.Generic;
using System.Linq;
using BaseBuilding.Scripts.Systems;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using BaseBuilding.Scripts.Systems.SaveSystem;
using BaseBuilding.scripts.util.common;
using BaseBuilding.Scripts.Util.objects;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public sealed partial class PipeSystem : Node3D, IPersistentManager
{
    [Export] private PackedScene _pipeJointScene = null!;
    [Export] private PackedScene _pipeScene = null!;
    [Export] private PackedScene _pipeTemporaryJointScene = null!;
    [Export] private PackedScene _temporaryPipeScene = null!;
    [Export] private Mesh _pipeJointMesh = null!;

    public readonly ResourceLineManager<PipeJoint, PipeConnector> PipeLineManager = new();
    private readonly ResourceLineRenderingManager _pipeLineRenderingManager = new();
    private bool _isEnabled;
    private Scripts.Systems.PipeSystem.PipePlacement.PipePlacerSystem? _pipePlacer;
    private readonly Dictionary<Eid, PipeJoint> _pipeJoints = new();
    private ulong _universalJointIdCounter = 1;

    private PipeSystem()
    {
    }

    public static PipeSystem Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        AddChild(_pipeLineRenderingManager);
    }

    public PipeJoint GetPipeJoint(Eid rid) => _pipeJoints[rid];

    public void RegisterPipeConnector(PipeConnector connector)
    {
        _pipeJoints.Add(connector.Eid, connector);
        connector.PipeAddedEvent += _onPipeAdded;
        connector.PipeRemovedEvent += _OnPipeRemoved;
        connector.TreeExiting += OnExitedTree;

        void OnExitedTree()
        {
            connector.PipeAddedEvent -= _onPipeAdded;
            connector.PipeRemovedEvent -= _OnPipeRemoved;
            connector.TreeExiting -= OnExitedTree;
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
        var processedJoints = new PipeJoint[pipeJoints.Length];

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
                    instance.SetId(GetNewJointEid());
                    instance.Position = globalPosition;
                    _commitPipeJoint(instance);
                    _sentJointToRenderingManager(instance);
                    return instance;
                }

                PipeJoint CreatePermanentPipeJointOnPipe(Pipe pipe, Vector3 globalPosition)
                {
                    var instance = _pipeJointScene.Instantiate<PipeJoint>();
                    instance.SetId(GetNewJointEid());
                    instance.OwnerPipe = pipe;
                    instance.Position = MathUtil.GetParallelPosition(pipe.GlobalTransform, globalPosition);
                    _commitPipeJoint(instance);
                    _sentJointToRenderingManager(instance);
                    pipe.FrontJoint.DisconnectFromJoint(pipe.BackJoint);
                    pipe.BackJoint.ConnectToJoint(instance);
                    pipe.FrontJoint.ConnectToJoint(instance);
                    var pipeLineId = (uint)(pipe.BackJoint.GetLineId() ?? pipe.FrontJoint.GetLineId())!;
                    PipeLineManager.AddJoint(pipeLineId, instance);
                    return instance;
                }
            }
        }

        void ConnectJoints(PipeJoint startPipeJoint, PipeJoint endPipeJoint)
        {
            startPipeJoint.ConnectToJoint(endPipeJoint);
            PipeLineManager.Connect(startPipeJoint, endPipeJoint);
        }
    }

    private void _commitPipeJoint(PipeJoint joint)
    {
        joint.PipeAddedEvent += _onPipeAdded;
        joint.PipeRemovedEvent += _OnPipeRemoved;
        joint.TreeExited += OnJointRemoved;
        AddChild(joint);
        _pipeJoints.Add(joint.Eid, joint);


        void OnJointRemoved()
        {
            joint.PipeAddedEvent -= _onPipeAdded;
            joint.PipeRemovedEvent -= _OnPipeRemoved;
            joint.TreeExited -= OnJointRemoved;
        }
    }

    private void _sentJointToRenderingManager(PipeJoint joint)
    {
        joint.SetRenderId(
            _pipeLineRenderingManager.AddJoint(
                GetWorld3D(),
                _pipeJointMesh,
                joint.GlobalTransform.TranslatedLocal(new Vector3(0.0f, joint.MeshOriginOffset, 0.0f))
            )
        );
    }

    private void _onPipeAdded(Pipe pipe)
    {
        AddChild(pipe);
        pipe.SetRenderId(
            _pipeLineRenderingManager.AddLimb(
                GetWorld3D(),
                pipe.CreateMesh(),
                pipe.GlobalTransform
            )
        );
    }

    private void _OnPipeRemoved(Pipe pipe)
    {
        _pipeLineRenderingManager.RemoveLimb(
            (uint)pipe.RenderId!,
            pipe.GlobalTransform
        );
        pipe.QueueFree();
    }


    public override void _ExitTree()
    {
        _pipeLineRenderingManager.Clean();
    }

    public void _AddSaveChild(Node child)
    {
        switch (child)
        {
            case PipeJoint pipeJoint:
                _commitPipeJoint(pipeJoint);
                _sentJointToRenderingManager(pipeJoint);
                break;
        }
    }

    public IPersistent[] _GetPersistentNodes()
    {
        // We ignore PipeConnectors as those are saved as part of the building they belong to.
        // ReSharper disable once CoVariantArrayConversion
        return _pipeJoints.Values.Where(e => e is not PipeConnector).ToArray();
    }

    public string _GetSavePath()
    {
        return "pipeSystem.save";
    }

    public void _AfterLoad()
    {
        if (_pipeJoints.Count == 0) return;

        var largestEid = _pipeJoints.MaxBy(e => e.Value.Eid).Value.Eid;
        _universalJointIdCounter = largestEid.Id + 1;
        var jointsGroupedByLineId = _pipeJoints.Values.GroupBy(joint => joint.GetLineId()).ToArray();

        var jointConnectionData = new Dictionary<Eid, List<Eid>>();
        foreach (var key in _pipeJoints.Keys)
        {
            jointConnectionData.Add(key, new List<Eid>());
        }

        foreach (var group in jointsGroupedByLineId)
        {
            if (group.Key == null) continue;
            var lineId = (uint)group.Key;
            PipeLineManager.CreateLine(lineId);

            var joints = group.ToArray();

            foreach (var pipeJoint in joints)
            {
                ConnectJoints(pipeJoint);
                PipeLineManager.AddBasedOnType(lineId, pipeJoint);
            }
        }

        void ConnectJoints(PipeJoint joint)
        {
            var thisData = jointConnectionData[joint.Eid];
            foreach (var connectedJointId in joint.ConnectedJointsIds)
            {
                if (thisData.Contains(connectedJointId)) continue;
                thisData.Add(connectedJointId);
                jointConnectionData[connectedJointId].Add(joint.Eid);
                var pipeJoint = GetPipeJoint(connectedJointId);
                joint.CreatePipesBetweenJoints(pipeJoint);
            }
        }
    }

    public Eid GetNewJointEid()
    {
        return new Eid(_universalJointIdCounter++);
    }
}