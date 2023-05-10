using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems;

public partial class ResourceLineRenderingManager : Node
{
    private const float MaxLimbClusterRadiusSquared = 100f * 100f;
    private const float MaxJointClusterRadiusSquared = 50f * 50f;
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
            renderInstance = _createRenderInstance(instanceId, world, mesh, globalTransform, 100, "Limb");
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
            renderInstance = _createRenderInstance(instanceId, world, mesh, globalTransform, 10, "Joint");
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
            return kv.Value.GlobalTransform.Origin.DistanceSquaredTo(globalTransform.Origin) <
                   MaxJointClusterRadiusSquared;
        }
    }

    private RenderInstance? _findCloseByLimbRenderInstance(Mesh mesh, Transform3D globalTransform)
    {
        var meshAabbZSize = mesh.GetAabb().Size.Z;
        foreach (var (_, value) in _limbRenderInstances)
        {
            var isCloseEnoughSquared = value.GlobalTransform.Origin.DistanceSquaredTo(
                globalTransform.Origin
            ) < MaxLimbClusterRadiusSquared;
            if (!isCloseEnoughSquared) continue;
            var isSameMesh = Mathf.IsEqualApprox(value.Mesh.GetAabb().Size.Z, meshAabbZSize);
            if (isSameMesh)
            {
                return value;
            }
        }

        return default;
    }

    private RenderInstance _createRenderInstance(
        uint id,
        World3D world,
        Mesh mesh,
        Transform3D globalTransform,
        int bufferSize,
        string type
    )
    {
        var multiMesh = RenderingServer.MultimeshCreate();
        RenderingServer.MultimeshAllocateData(multiMesh, 1, RenderingServer.MultimeshTransformFormat.Transform3D);
        RenderingServer.MultimeshSetMesh(multiMesh, mesh.GetRid());

        var instance = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetBase(instance, multiMesh);
        RenderingServer.InstanceSetScenario(instance, world.Scenario);
        RenderingServer.InstanceSetTransform(instance, globalTransform);
        var renderInstance = new RenderInstance(id, mesh, globalTransform, multiMesh, instance, bufferSize);
        var meshInstance = new MeshInstance3D();
        var box = new BoxMesh();
        box.Size = new Vector3(0.2f, 1.0f, 0.2f);
        meshInstance.Mesh = box;
        meshInstance.Position = globalTransform.Origin;
        var label = new Label3D();
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        label.Text = $"{type}: {id}";
        label.Position = new Vector3(0.0f, 2.0f, 0.0f);
        meshInstance.AddChild(label);
        AddChild(meshInstance);
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
        private int _bufferSize;
        private int currentSize = 0;

        public readonly uint Id;

        public RenderInstance(
            uint id,
            Mesh mesh,
            Transform3D globalTransform,
            Rid multiMesh,
            Rid instance,
            int bufferSize
        )
        {
            Id = id;
            Mesh = mesh;
            GlobalTransform = globalTransform;
            MultiMesh = multiMesh;
            Instance = instance;
            _bufferSize = bufferSize;
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
            if (InstanceCount > currentSize)
            {
                currentSize = InstanceCount + _bufferSize;
                RenderingServer.MultimeshAllocateData(
                    MultiMesh,
                    currentSize,
                    RenderingServer.MultimeshTransformFormat.Transform3D
                );
            }

            _updateMultimeshInstanceTransforms();
        }


        /// <summary>
        /// This has the same function as RenderingServer.MultimeshInstanceSetTransform() but instead of having to call
        /// it for each instance, it sets all the transforms at once, which is much faster.
        /// </summary>
        private void _updateMultimeshInstanceTransforms()
        {
            var buffer = new float[currentSize * 12]; // Each transform buffer has 12 floats
            var bufferIdx = 0;

            foreach (var transform3D in CollectionsMarshal.AsSpan(_transforms))
            {
                buffer[bufferIdx] = transform3D.Basis.Row0.X;
                buffer[bufferIdx + 1] = transform3D.Basis.Row0.Y;
                buffer[bufferIdx + 2] = transform3D.Basis.Row0.Z;
                buffer[bufferIdx + 3] = transform3D.Origin.X;
                buffer[bufferIdx + 4] = transform3D.Basis.Row1.X;
                buffer[bufferIdx + 5] = transform3D.Basis.Row1.Y;
                buffer[bufferIdx + 6] = transform3D.Basis.Row1.Z;
                buffer[bufferIdx + 7] = transform3D.Origin.Y;
                buffer[bufferIdx + 8] = transform3D.Basis.Row2.X;
                buffer[bufferIdx + 9] = transform3D.Basis.Row2.Y;
                buffer[bufferIdx + 10] = transform3D.Basis.Row2.Z;
                buffer[bufferIdx + 11] = transform3D.Origin.Z;
                bufferIdx += 12;
            }


            RenderingServer.MultimeshSetBuffer(MultiMesh, buffer);
        }

        public void Clean()
        {
            RenderingServer.FreeRid(MultiMesh);
            RenderingServer.FreeRid(Instance);
        }
    }
}