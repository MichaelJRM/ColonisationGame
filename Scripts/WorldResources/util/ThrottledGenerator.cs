using Godot;

namespace BaseBuilding.Scripts.WorldResources.util;

public class ThrottledGenerator
{
    private readonly float _generationRatePerGameSecond;
    private float _extractedSinceLastGeneration;
    private double _lastGenerationTimestampInSeconds;

    public ThrottledGenerator(float generationRatePerGameSecond, double gameTimeInSeconds)
    {
        _generationRatePerGameSecond = generationRatePerGameSecond;
        _lastGenerationTimestampInSeconds = gameTimeInSeconds;
    }

    public float Generate(float amount, double gameTimeInSeconds)
    {
        var secondsSinceLastGeneration = (float)(gameTimeInSeconds - _lastGenerationTimestampInSeconds);
        _lastGenerationTimestampInSeconds = gameTimeInSeconds;
        if (secondsSinceLastGeneration >= 1f)
        {
            _extractedSinceLastGeneration = 0f;
        }

        var amountToGenerate = Mathf.Min(amount, _generationRatePerGameSecond - _extractedSinceLastGeneration);
        _extractedSinceLastGeneration += amountToGenerate;
        return amountToGenerate;
    }
}