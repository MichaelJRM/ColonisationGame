using System;
using BaseBuilding.Scripts.Systems.SaveSystem;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.ResourceDeposit;

public partial class ResourceDeposit : PersistentArea3D<ResourceDeposit.SerializationData>
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

    public override object Save()
    {
        return new SerializationData(
            ca: CurrentAmount
        );
    }

    public override void Load()
    {
        CurrentAmount = SaveContent!.Ca;
    }

    public override bool InstantiateOnLoad()
    {
        return false;
    }

    public class SerializationData
    {
        public SerializationData(float ca)
        {
            Ca = ca;
        }

        /// <summary>
        /// CurrentAmount
        /// </summary>
        public float Ca { get; set; }
    }
}