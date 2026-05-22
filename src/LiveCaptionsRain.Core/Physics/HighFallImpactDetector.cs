namespace LiveCaptionsRain.Core.Physics;

public static class HighFallImpactDetector
{
    public const double FallDistanceRatio = 0.3d;

    private const double LandingTolerancePixels = 32d;
    private const double FloorTolerancePixels = 4d;

    public static bool ShouldFracture(
        PhysicsWordSnapshot word,
        double screenHeight,
        IEnumerable<PhysicsRect> windowTopPlatforms)
    {
        if (!word.HighFallFractureEnabled || screenHeight <= 0)
        {
            return false;
        }

        if (word.FallDistancePixels < screenHeight * FallDistanceRatio)
        {
            return false;
        }

        if (word.Bounds.Bottom >= screenHeight - FloorTolerancePixels)
        {
            return true;
        }

        return windowTopPlatforms.Any(platform => IsLandingOnPlatform(word.Bounds, platform));
    }

    private static bool IsLandingOnPlatform(PhysicsRect wordBounds, PhysicsRect platform)
    {
        if (platform.Width <= 0)
        {
            return false;
        }

        var horizontallyOverlaps = wordBounds.Left < platform.Right && wordBounds.Right > platform.Left;
        if (!horizontallyOverlaps)
        {
            return false;
        }

        return wordBounds.Bottom >= platform.Top - FloorTolerancePixels
            && wordBounds.Bottom <= platform.Top + LandingTolerancePixels;
    }
}
