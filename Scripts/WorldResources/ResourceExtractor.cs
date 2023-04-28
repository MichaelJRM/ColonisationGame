using System;
using BaseBuilding.scripts.singletons;
using BaseBuilding.Scripts.WorldResources.util;
using Godot;

namespace BaseBuilding.Scripts.WorldResources;

public partial class ResourceExtractor : Node3D
{
    [Export] private float _extractionRate;
    private double _gameTimeStamp;
    private Global _global = null!;
    private ThrottledGenerator _throttledGenerator = null!;
    [Export] protected WorldResource Resource = null!;
    protected ResourceDeposit.ResourceDeposit? ResourceDeposit;


    public override void _Ready()
    {
        if (Resource == null) throw new Exception("ResourceExtractor: Resource is null!");
        _global = GetNode<Global>("/root/Global");
        _gameTimeStamp = _global.GameTimeInSeconds;
        _throttledGenerator = new ThrottledGenerator(_extractionRate, _global.GameTimeInSeconds);
    }

    protected float Extract(float amount)
    {
        if (ResourceDeposit == null) return 0f;
        var amountAvailable = _throttledGenerator.Generate(amount, _global.GameTimeInSeconds);
        var amountExtracted = ResourceDeposit.Take(amountAvailable);
        return amountExtracted;
    }
}