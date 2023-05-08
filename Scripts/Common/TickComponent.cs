using System;
using Godot;

namespace BaseBuilding.scripts.common;

public partial class TickComponent : Node
{
    private Action? _onTick;
    private Action? _onPhysicsTick;
    private float _tickRate = 1f;
    private double _timeSinceLastTick;


    public void Pause()
    {
        SetProcess(false);
    }

    public void Resume()
    {
        SetProcess(true);
    }

    /// <summary>
    /// The lower the tickRateInSeconds, the more often the tick will occur.
    /// </summary>
    /// <param name="tickRateInSeconds"></param>
    public void SetTickRateInSeconds(float tickRateInSeconds)
    {
        _tickRate = tickRateInSeconds;
    }

    /// <summary>
    /// The tick rate in frames per second.
    /// </summary>
    /// <param name="fps"></param>
    public void SetTickRateInFps(int fps)
    {
        _tickRate = 1f / fps;
    }

    public void SetOnTick(Action onTick)
    {
        _onTick = onTick;
    }

    public override void _Process(double delta)
    {
        _tickProcess(delta);
    }

    private void _tickProcess(double delta)
    {
        _timeSinceLastTick += delta;
        if (_timeSinceLastTick < _tickRate) return;
        _timeSinceLastTick = 0;

        _onTick!.Invoke();
    }
}