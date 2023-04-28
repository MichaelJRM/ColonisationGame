namespace BaseBuilding.Scripts.Systems.EnergySystem.WireConnector;

public delegate float FetchEnergyCallback(float amount);

public partial class WireOutputConnector : WireConnector
{
    private FetchEnergyCallback _fetchEnergyCallback = null!;

    public void Activate(FetchEnergyCallback fetchEnergyCallback)
    {
        _fetchEnergyCallback = fetchEnergyCallback;
    }

    public float FetchEnergy(float amount)
    {
        return _fetchEnergyCallback.Invoke(amount);
    }
}