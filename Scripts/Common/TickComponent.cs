using System;
using Godot;

namespace BaseBuilding.scripts.common;

public partial class TickComponent : Node
{
    private Action _onTick = () => { };
    private float _tickRate = 1f;
    private double _timeSinceLastTick;


    /// <summary>
    ///     The higher the tick rate, the more often the tick will occur.
    /// </summary>
    /// <param name="tickRate"></param>
    public void SetTickRate(float tickRate)
    {
        _tickRate = 1f / tickRate;
    }

    public void SetOnTick(Action onTick)
    {
        _onTick = onTick;
    }

    public override void _Process(double delta)
    {
        _tick(delta);
    }

    private void _tick(double delta)
    {
        _timeSinceLastTick += delta;
        if (_timeSinceLastTick < _tickRate) return;
        _timeSinceLastTick = 0;
        _onTick();
    }
}