using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

[Tool]
public partial class WireInputOutputConnector : WireConnector, IResourceOutputConnector, IResourceInputConnector
{
    private bool _hasBeenActivated = false;
    private OnResourceRequestedCallback? _onResourceRequestedCallback;

    public new void Activate()
    {
        if (!_hasBeenActivated)
        {
            _hasBeenActivated = true;
            base.Activate();
        }
    }

    public void BindSource(OnResourceRequestedCallback onResourceRequestedCallback)
    {
        _onResourceRequestedCallback = onResourceRequestedCallback;
    }

    public float RequestResource(float amount)
    {
        return _onResourceRequestedCallback!.Invoke(amount, this);
    }

    private ResourceAskedCallback _resourceAskedCallback = null!;

    public void BindOnResourceAsked(ResourceAskedCallback resourceLineOutputConnector)
    {
        _resourceAskedCallback = resourceLineOutputConnector;
    }

    public float AskForResource(float amount)
    {
        return _resourceAskedCallback.Invoke(amount);
    }
}