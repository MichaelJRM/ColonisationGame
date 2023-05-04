using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Systems;

public interface IResourceConnector : IResourceJoint
{
    public void Activate();

    object GetOwner();

    WorldResource GetAcceptedResource();

    bool AcceptsResource(WorldResource worldResource);
}

public delegate float OnResourceRequestedCallback(
    float amount,
    IResourceInputConnector inputConnector
);

public interface IResourceInputConnector : IResourceConnector
{
    public void BindSource(OnResourceRequestedCallback onResourceRequestedCallback);

    public float RequestResource(float amount);
}

public delegate float ResourceAskedCallback(float amount);

public interface IResourceOutputConnector : IResourceConnector
{
    public void BindOnResourceAsked(ResourceAskedCallback resourceLineOutputConnector);
    public float AskForResource(float amount);
}