using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

[Tool]
public partial class WireInputConnector : WireConnector, IResourceInputConnector
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