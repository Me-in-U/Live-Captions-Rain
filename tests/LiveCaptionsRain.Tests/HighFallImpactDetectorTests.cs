using LiveCaptionsRain.Core.Physics;

namespace LiveCaptionsRain.Tests;

public sealed class HighFallImpactDetectorTests
{
    [Fact]
    public void ShouldFracture_requires_twenty_five_percent_fall_before_floor_impact()
    {
        var word = Word(fallDistancePixels: 250, bounds: new PhysicsRect(100, 950, 120, 50));

        Assert.True(HighFallImpactDetector.ShouldFracture(word, screenHeight: 1000, []));
    }

    [Fact]
    public void ShouldFracture_ignores_short_falls_even_when_landed()
    {
        var word = Word(fallDistancePixels: 249, bounds: new PhysicsRect(100, 950, 120, 50));

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
    public void ShouldFracture_uses_other_words_as_landing_surfaces()
    {
        var word = Word(fallDistancePixels: 330, bounds: new PhysicsRect(140, 370, 80, 30));
        var stackedWordTop = new PhysicsRect(120, 398, 120, 1);

        Assert.True(HighFallImpactDetector.ShouldFracture(word, screenHeight: 1000, [], [stackedWordTop]));
    }

    [Fact]
    public void ShouldFracture_uses_other_word_bounds_even_after_deep_overlap()
    {
        var word = Word(fallDistancePixels: 330, bounds: new PhysicsRect(140, 370, 80, 80));
        var stackedWord = new PhysicsRect(120, 398, 120, 50);

        Assert.True(HighFallImpactDetector.ShouldFracture(word, screenHeight: 1000, [], [stackedWord]));
    }

    [Fact]
    public void ShouldFracture_ignores_other_words_when_fall_word_is_not_above_them()
    {
        var word = Word(fallDistancePixels: 330, bounds: new PhysicsRect(140, 430, 80, 80));
        var stackedWord = new PhysicsRect(120, 398, 120, 50);

        Assert.False(HighFallImpactDetector.ShouldFracture(word, screenHeight: 1000, [], [stackedWord]));
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
