using System.Collections.Generic;

namespace BaseBuilding.Scripts.Systems;

public class ResourceLineManager<TJoint, TConnector> where TJoint : IResourceJoint where TConnector : IResourceConnector
{
    private readonly Dictionary<uint, ResourceLine<TJoint, TConnector>> _lines = new();
    private uint _lineIdCounter;

    public uint CreateLine()
    {
        var line = new ResourceLine<TJoint, TConnector>(_lineIdCounter);
        _lines.Add(_lineIdCounter, line);
        return _lineIdCounter++;
    }

    public void AddJoint(uint lineId, TJoint joint)
    {
        var resourceLine = _lines[lineId];
        resourceLine.AddJoint(joint);
        joint.SetLineId(lineId);
    }

    public void AddConnector(uint lineId, TConnector connector)
    {
        var resourceLine = _lines[lineId];
        resourceLine.AddConnector(connector);
        connector.SetLineId(lineId);
    }

    public void MergeLines(uint lineA, uint lineB)
    {
        var resourceLineA = _lines[lineA];
        var resourceLineB = _lines[lineB];
        resourceLineA.MergeWith(resourceLineB);
        _lines.Remove(lineB);
    }

    public void Connect(TJoint first, TJoint second)
    {
        if (
            first.IsConnectedToLine()
            && second.IsConnectedToLine()
            && first.GetLineId() != second.GetLineId())
        {
            MergeLines(
                (uint)first.GetLineId()!,
                (uint)second.GetLineId()!
            );
            return;
        }

        if (!first.IsConnectedToLine() && !second.IsConnectedToLine())
        {
            var pipeLineId = CreateLine();
            _addBasedOnType(pipeLineId, first);
            _addBasedOnType(pipeLineId, second);
            return;
        }

        if (first.IsConnectedToLine())
        {
            var pipeLineId = (uint)first.GetLineId()!;
            _addBasedOnType(pipeLineId, second);
            return;
        }

        if (!first.IsConnectedToLine())
        {
            var pipeLineId = (uint)second.GetLineId()!;
            _addBasedOnType(pipeLineId, first);
            return;
        }
    }

    private void _addBasedOnType(uint lineId, TJoint joint)
    {
        if (joint is TConnector connector)
        {
            AddConnector(lineId, connector);
        }
        else
        {
            AddJoint(lineId, joint);
        }
    }
}