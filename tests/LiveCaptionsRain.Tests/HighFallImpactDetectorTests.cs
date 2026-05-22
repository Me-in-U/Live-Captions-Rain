using LiveCaptionsRain.Core.Physics;

namespace LiveCaptionsRain.Tests;

public sealed class HighFallImpactDetectorTests
{
    [Fact]
    public void ShouldFracture_requires_thirty_percent_fall_before_floor_impact()
    {
        var word = Word(fallDistancePixels: 350, bounds: new PhysicsRect(100, 950, 120, 50));

        Assert.True(HighFallImpactDetector.ShouldFracture(word, screenHeight: 1000, []));
    }

    [Fact]
    public void ShouldFracture_ignores_short_falls_even_when_landed()
    {
        var word = Word(fallDistancePixels: 299, bounds: new PhysicsRect(100, 950, 120, 50));

        Assert.False(HighFallImpactDetector.ShouldFracture(word, screenHeight: 1000, []));
    }

    [Fact]
    public void ShouldFracture_uses_visible_window_platforms_as_landing_surfaces()
    {
        var word = Word(fallDistancePixels: 320, bounds: new PhysicsRect(140, 370, 80, 30));
        var platform = new PhysicsRect(100, 398, 200, 1);

        Assert.True(HighFallImpactDetector.ShouldFracture(word, screenHeight: 1000, [platform]));
    }

    [Fact]
    public void ShouldFracture_ignores_disabled_fragment_words()
    {
        var word = Word(fallDistancePixels: 600, bounds: new PhysicsRect(100, 950, 120, 50), enabled: false);

        Assert.False(HighFallImpactDetector.ShouldFracture(word, screenHeight: 1000, []));
    }

    private static PhysicsWordSnapshot Word(double fallDistancePixels, PhysicsRect bounds, bool enabled = true)
    {
        return new PhysicsWordSnapshot(
            Guid.NewGuid(),
            "captioned",
            bounds,
            AngleRadians: 0,
            DateTimeOffset.UtcNow,
            Opacity: 1,
            FallDistancePixels: fallDistancePixels,
            HighFallFractureEnabled: enabled);
    }
}
