namespace BaseBuilding.Scripts.Systems;

public interface IResourceJoint
{
    void SetLineId(uint? lineId);

    uint? GetLineId();

    bool IsConnectedToLine();

    bool CanConnect();
}