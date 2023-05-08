using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

[Tool]
public partial class PipeInputConnector : PipeConnector, IResourceInputConnector
{
    public float RequestResource(float amount)
    {
        return scripts.systems.PipeSystem.PipeSystem.Instance.PipeLineManager.RequestResource(amount, this);
    }
}