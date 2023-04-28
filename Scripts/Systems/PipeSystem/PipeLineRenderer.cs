using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipeLineRenderer : RefCounted
{
    private const float MultiMeshMaxRenderingRadius = 200.0f;
    private readonly Dictionary<int, RenderInstance> _jointRenderInstances = new();
    private readonly Dictionary<int, RenderInstance> _pipeRenderInstances = new();
    private int _jointRenderInstanceIdCounter;
    private int _pipeRenderInstanceIdCounter;

    public int AddPipe(World3D world, Mesh mesh, Transform3D globalTransform)
    {
        var renderInstance = _findCloseByPipeRenderInstance(mesh, globalTransform);
        int instanceId;
        if (renderInstance == null)
        {
            instanceId = _pipeRenderInstanceIdCounter;
            _pipeRenderInstanceIdCounter++;
            renderInstance = _createRenderInstance(instanceId, world, mesh, globalTransform);
            _pipeRenderInstances[instanceId] = renderInstance;
        }
        else
        {
            renderInstance.AddChildInstance(globalTransform);
            instanceId = renderInstance.Id;
        }

        return instanceId;
    }

    public void RemovePipe(int renderInstanceId, Transform3D transform3D)
    {
        var renderInstance = _pipeRenderInstances[renderInstanceId];
        renderInstance.RemoveChildInstance(transform3D);
    }

    public int AddJoint(World3D world, Mesh mesh, Transform3D globalTransform)
    {
        var renderInstance = _jointRenderInstances.FirstOrDefault(
            e => e.Value.GlobalTransform.Origin.DistanceTo(globalTransform.Origin) < MultiMeshMaxRenderingRadius
        ).Value;
        int instanceId;
        if (renderInstance == null)
        {
            instanceId = _jointRenderInstanceIdCounter;
            _jointRenderInstanceIdCounter++;
            renderInstance = _createRenderInstance(instanceId, world, mesh, globalTransform);
            _jointRenderInstances[instanceId] = renderInstance;
        }
        else
        {
            renderInstance.AddChildInstance(globalTransform);
            instanceId = renderInstance.Id;
        }

        return instanceId;
    }

    private RenderInstance? _findCloseByPipeRenderInstance(Mesh mesh, Transform3D globalTransform)
    {
        foreach (var (_, value) in _pipeRenderInstances)
        {
            var isSameMesh = Mathf.IsEqualApprox(value.Mesh.GetAabb().Size.Z, mesh.GetAabb().Size.Z);
            var isCloseEnough = value.GlobalTransform.Origin.DistanceTo(globalTransform.Origin) <
                                MultiMeshMaxRenderingRadius;
            if (isSameMesh && isCloseEnough) return value;
        }

        return default;
    }

    private RenderInstance _createRenderInstance(int id, World3D world, Mesh mesh, Transform3D globalTransform)
    {
        var multiMesh = RenderingServer.MultimeshCreate();
        RenderingServer.MultimeshAllocateData(multiMesh, 1, RenderingServer.MultimeshTransformFormat.Transform3D);
        RenderingServer.MultimeshSetMesh(multiMesh, mesh.GetRid());

        var instance = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetBase(instance, multiMesh);
        RenderingServer.InstanceSetScenario(instance, world.Scenario);
        RenderingServer.InstanceSetTransform(instance, globalTransform);
        var renderInstance = new RenderInstance(id, mesh, globalTransform, multiMesh, instance);
        renderInstance.AddChildInstance(globalTransform);
        return renderInstance;
    }

    public void Clean()
    {
        foreach (var (_, value) in _pipeRenderInstances) value.Clean();
        _pipeRenderInstances.Clear();
    }

    private class RenderInstance
    {
        private readonly List<Transform3D> _transforms = new();

        public readonly int Id;

        public RenderInstance(int id, Mesh mesh, Transform3D globalTransform, Rid multiMesh, Rid instance)
        {
            Id = id;
            Mesh = mesh;
            GlobalTransform = globalTransform;
            MultiMesh = multiMesh;
            Instance = instance;
        }

        public Mesh Mesh { get; }
        public Transform3D GlobalTransform { get; }
        private Rid MultiMesh { get; }
        private Rid Instance { get; }
        private int InstanceCount => _transforms.Count;

        public void AddChildInstance(Transform3D transform)
        {
            _transforms.Add(MathUtil.ToLocal(GlobalTransform, transform));
            _update();
        }

        public void RemoveChildInstance(Transform3D transform)
        {
            var toLocal = MathUtil.ToLocal(GlobalTransform, transform);
            _transforms.Remove(toLocal);
            _update();
        }

        private void _update()
        {
            RenderingServer.MultimeshAllocateData(
                MultiMesh,
                InstanceCount,
                RenderingServer.MultimeshTransformFormat.Transform3D
            );

            for (var i = 0; i < _transforms.Count; i++)
                RenderingServer.MultimeshInstanceSetTransform(MultiMesh, i, _transforms[i]);
        }

        public void Clean()
        {
            RenderingServer.FreeRid(MultiMesh);
            RenderingServer.FreeRid(Instance);
        }
    }
}