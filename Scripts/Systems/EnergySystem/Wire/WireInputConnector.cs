using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

[Tool]
public partial class WireInputConnector : WireConnector, IResourceInputConnector
{
    public float RequestResource(float amount)
    {
        return EnergySystem.Instance.WireLineManager.RequestResource(amount, this);
    }
}