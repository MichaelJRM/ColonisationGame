using System;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.ResourceDeposit;

public partial class ResourceDeposit : Area3D
{
    [Export] private float _initialAmount;
    [Export] public WorldResource Resource { get; private set; } = null!;
    public float CurrentAmount { get; private set; }


    public override void _Ready()
    {
        if (Resource == null) throw new Exception("ResourceDeposit: Resource not assigned!");
        CurrentAmount = _initialAmount;
    }

    public float Take(float amount)
    {
        var newAmount = Mathf.Max(CurrentAmount - amount, 0f);
        var amountTaken = CurrentAmount - newAmount;
        CurrentAmount = newAmount;
        return amountTaken;
    }
}