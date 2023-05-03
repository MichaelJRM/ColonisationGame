using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems;

public class ResourceLineRenderer
{
    private const float MaxClusterRadius = 200.0f;
    private readonly Dictionary<uint, RenderInstance> _jointRenderInstances = new();
    private readonly Dictionary<uint, RenderInstance> _limbRenderInstances = new();
    private uint _jointRenderInstanceIdCounter;
    private uint _limbRenderInstanceIdCounter;

    public uint AddLimb(World3D world, Mesh mesh, Transform3D globalTransform)
    {
        var renderInstance = _findCloseByLimbRenderInstance(mesh, globalTransform);
        uint instanceId;
        if (renderInstance == null)
        {
            instanceId = _limbRenderInstanceIdCounter;
            _limbRenderInstanceIdCounter++;
            renderInstance = _createRenderInstance(instanceId, world, mesh, globalTransform);
            _limbRenderInstances[instanceId] = renderInstance;
        }
        else
        {
            renderInstance.AddChildInstance(globalTransform);
            instanceId = renderInstance.Id;
        }

        return instanceId;
    }

    public void RemoveLimb(uint renderInstanceId, Transform3D transform3D)
    {
        var renderInstance = _limbRenderInstances[renderInstanceId];
        renderInstance.RemoveChildInstance(transform3D);
    }

    public uint AddJoint(World3D world, Mesh mesh, Transform3D globalTransform)
    {
        var renderInstance = _jointRenderInstances.FirstOrDefault(InstanceInRange).Value;
        uint instanceId;
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

        bool InstanceInRange(KeyValuePair<uint, RenderInstance> kv)
        {
            return kv.Value.GlobalTransform.Origin.DistanceTo(globalTransform.Origin) < MaxClusterRadius;
        }
    }

    private RenderInstance? _findCloseByLimbRenderInstance(Mesh mesh, Transform3D globalTransform)
    {
        foreach (var (_, value) in _limbRenderInstances)
        {
            var isSameMesh = Mathf.IsEqualApprox(value.Mesh.GetAabb().Size.Z, mesh.GetAabb().Size.Z);
            var isCloseEnough = value.GlobalTransform.Origin.DistanceTo(globalTransform.Origin) < MaxClusterRadius;
            if (isSameMesh && isCloseEnough) return value;
        }

        return default;
    }

    private RenderInstance _createRenderInstance(uint id, World3D world, Mesh mesh, Transform3D globalTransform)
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
        foreach (var (_, value) in _jointRenderInstances) value.Clean();
        _jointRenderInstances.Clear();
        foreach (var (_, value) in _limbRenderInstances) value.Clean();
        _limbRenderInstances.Clear();
    }

    private class RenderInstance
    {
        private readonly List<Transform3D> _transforms = new();

        public readonly uint Id;

        public RenderInstance(uint id, Mesh mesh, in Transform3D globalTransform, Rid multiMesh, Rid instance)
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