namespace LiveCaptionsRain.Core.Physics;

public readonly record struct WindVector(double HorizontalPixelsPerSecond, double VerticalPixelsPerSecond)
{
    public static WindVector None { get; } = new(0, 0);
}
