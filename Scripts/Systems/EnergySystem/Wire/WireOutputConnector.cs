using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

[Tool]
public partial class WireOutputConnector : WireConnector, IResourceOutputConnector
{
    private ResourceAskedCallback _resourceAskedCallback = null!;

    public void BindOnResourceAsked(ResourceAskedCallback resourceLineOutputConnector)
    {
        _resourceAskedCallback = resourceLineOutputConnector;
    }

    public float AskForResource(float amountPerConnector)
    {
        return _resourceAskedCallback.Invoke(amountPerConnector);
    }
}