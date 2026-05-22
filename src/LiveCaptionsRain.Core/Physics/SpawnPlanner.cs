using LiveCaptionsRain.Core.Settings;
using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Core.Physics;

public sealed class SpawnPlanner(int? randomSeed = null)
{
    private const double SequentialGapMultiplier = 1.25d;

    private readonly Random _random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
    private int _sequenceIndex;

    public double NextX(SpawnMode mode, ScreenRect bounds, double bodyWidth)
    {
        var halfWidth = bodyWidth / 2d;
        var min = bounds.Left + halfWidth;
        var max = bounds.Right - halfWidth;

        if (max <= min)
        {
            return bounds.Left + bounds.Width / 2d;
        }

        return mode switch
        {
            SpawnMode.LeftToRight => Sequential(min, max, bodyWidth, forward: true),
            SpawnMode.RightToLeft => Sequential(min, max, bodyWidth, forward: false),
            SpawnMode.CenterBiased => CenterBiased(min, max),
            _ => min + _random.NextDouble() * (max - min)
        };
    }

    private double Sequential(double min, double max, double bodyWidth, bool forward)
    {
        var span = max - min;
        var step = Math.Clamp(bodyWidth * SequentialGapMultiplier, 48, Math.Max(48, span));
        var slots = Math.Max(1, (int)Math.Floor(span / step) + 1);
        var slot = _sequenceIndex++ % slots;
        var value = Math.Min(max, min + slot * step);
        return forward ? value : max - (value - min);
    }

    private double CenterBiased(double min, double max)
    {
        var center = (min + max) / 2d;
        var span = max - min;
        var gaussianish = (_random.NextDouble() + _random.NextDouble() + _random.NextDouble()) / 3d;
        var offset = (gaussianish - 0.5d) * span * 0.72d;
        return Math.Clamp(center + offset, min, max);
    }
}
