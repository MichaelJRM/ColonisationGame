using System;
using System.Collections.Generic;

namespace BaseBuilding.scripts.systems.PipeSystem;

public class PipeLineManager
{
    private readonly Dictionary<uint, PipeLine> _pipeLines = new();
    private uint _pipeLineIdCounter;

    public uint CreatePipeLine()
    {
        _pipeLines.Add(_pipeLineIdCounter, new PipeLine(_pipeLineIdCounter));
        _pipeLineIdCounter++;
        return _pipeLineIdCounter - 1;
    }

    public void AddPipeJoint(PipeJoint pipeJoint, uint pipeLineId)
    {
        var pipeLine = _pipeLines[pipeLineId];
        pipeLine.AddPipeJoint(pipeJoint);
        pipeJoint.SetPipeLineId(pipeLineId);
    }

    public void MergePipeLines(uint pipeLineIdA, uint pipeLineIdB)
    {
        var pipeLineA = _pipeLines[pipeLineIdA];
        var pipeLineB = _pipeLines[pipeLineIdB];
        pipeLineA.MergeWith(pipeLineB);
        if (!_pipeLines.Remove(pipeLineIdB)) throw new Exception("Tried to remove a pipe line that doesn't exist");
    }

    public PipeLine GetPipeLine(uint id)
    {
        return _pipeLines[id];
    }
}