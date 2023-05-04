namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

public partial class PipeOutputConnector : PipeConnector, IResourceOutputConnector
{
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