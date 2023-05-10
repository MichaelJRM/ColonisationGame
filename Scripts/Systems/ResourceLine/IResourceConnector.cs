using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Systems;

public interface IResourceConnector : IResourceJoint
{
    public void Activate();

    object GetOwner();

    WorldResource GetAcceptedResource();

    bool IsConnected();

    bool AcceptsResource(WorldResource worldResource);
}

public interface IResourceInputConnector : IResourceConnector
{
    public float RequestResource(float amount);
}

public delegate float ResourceAskedCallback(float amount);

public interface IResourceOutputConnector : IResourceConnector
{
    /// <summary>
    /// Bind a callback to be called when this connector is asked for a resource.
    /// </summary>
    /// <param name="resourceLineOutputConnector"></param>
    public void BindOnResourceAsked(ResourceAskedCallback resourceLineOutputConnector);

    public float AskForResource(float amount);
}