using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

[Tool]
public partial class WireInputOutputConnector : WireConnector, IResourceOutputConnector, IResourceInputConnector
{
    private bool _hasBeenActivated = false;

    public new void Activate()
    {
        if (!_hasBeenActivated)
        {
            _hasBeenActivated = true;
            base.Activate();
        }
    }

    public float RequestResource(float amount)
    {
        return EnergySystem.Instance.WireLineManager.RequestResource(amount, this);
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