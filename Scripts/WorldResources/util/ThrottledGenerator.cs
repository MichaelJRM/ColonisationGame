using Godot;

namespace BaseBuilding.Scripts.WorldResources.util;

public class ThrottledGenerator
{
    private readonly float _generationRatePerGameSecond;
    private double _lastGenerationTimestamp;

    public ThrottledGenerator(float generationRatePerGameSecond, double gameTimeInSeconds)
    {
        _generationRatePerGameSecond = generationRatePerGameSecond;
        _lastGenerationTimestamp = gameTimeInSeconds;
    }

    public float Generate(float amount, double gameTimeInSeconds)
    {
        var timeSinceLastGeneration = (float)(gameTimeInSeconds - _lastGenerationTimestamp);
        _lastGenerationTimestamp = gameTimeInSeconds;
        var remainingGenerationPercentage = Mathf.Min(timeSinceLastGeneration / _generationRatePerGameSecond, 1.0f);
        var amountAvailable = _generationRatePerGameSecond * remainingGenerationPercentage;
        var amountToReturn = Mathf.Min(amount, amountAvailable);
        return amountToReturn;
    }
}