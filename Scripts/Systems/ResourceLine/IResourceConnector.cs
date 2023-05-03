using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Systems;

public interface IResourceConnector : IResourceJoint
{
    bool AcceptsResource(WorldResource resource);


    public void Activate();

    object GetOwner();
}

public delegate float OnResourceRequestedCallback(
    WorldResource worldResource,
    float amount,
    IResourceInputConnector inputConnector
);

public interface IResourceInputConnector : IResourceConnector
{
    public void BindOnResourceRequested(OnResourceRequestedCallback onResourceRequestedCallback);

    public float RequestResource(WorldResource resource);
}

public delegate float ResourceAskedCallback(float amount);

public interface IResourceOutputConnector : IResourceConnector
{
    public void BindOnResourceAsked(ResourceAskedCallback resourceLineOutputConnector);
    public float AskForResource(WorldResource worldResource, float amountPerConnector);
}