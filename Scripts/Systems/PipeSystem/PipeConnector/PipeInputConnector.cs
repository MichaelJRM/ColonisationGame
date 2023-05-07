using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

[Tool]
public partial class PipeInputConnector : PipeConnector, IResourceInputConnector
{
    private OnResourceRequestedCallback? _onResourceRequestedCallback;


    public void BindSource(OnResourceRequestedCallback onResourceRequestedCallback)
    {
        _onResourceRequestedCallback = onResourceRequestedCallback;
    }

    public float RequestResource(float amount)
    {
        return _onResourceRequestedCallback!.Invoke(amount, this);
    }
}