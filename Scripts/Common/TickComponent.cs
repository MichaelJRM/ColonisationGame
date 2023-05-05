using System;
using Godot;

namespace BaseBuilding.scripts.common;

public partial class TickComponent : Node
{
    private Action? _onTick;
    private Action? _onPhysicsTick;
    private float _tickRate = 1f;
    private double _timeSinceLastTick;


    public override void _Ready()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
    }


    public void Pause()
    {
        if (_onTick != null)
        {
            SetProcess(false);
        }

        if (_onPhysicsTick != null)
        {
            SetPhysicsProcess(false);
        }
    }

    public void Resume()
    {
        if (_onTick != null)
        {
            SetProcess(true);
        }

        if (_onPhysicsTick != null)
        {
            SetPhysicsProcess(true);
        }
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

    public void SetOnPhysicsTick(Action onTick)
    {
        _onPhysicsTick = onTick;
    }

    public override void _Process(double delta)
    {
        _tickProcess(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _tickPhysicsProcess(delta);
    }

    private void _tickProcess(double delta)
    {
        _timeSinceLastTick += delta;
        if (_timeSinceLastTick < _tickRate) return;
        _timeSinceLastTick = 0;

        _onTick!.Invoke();
    }

    private void _tickPhysicsProcess(double delta)
    {
        _timeSinceLastTick += delta;
        if (_timeSinceLastTick < _tickRate) return;
        _timeSinceLastTick = 0;

        _onPhysicsTick!.Invoke();
    }
}