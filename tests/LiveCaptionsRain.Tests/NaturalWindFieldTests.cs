using LiveCaptionsRain.Core.Physics;

namespace LiveCaptionsRain.Tests;

public sealed class NaturalWindFieldTests
{
    [Fact]
    public void Sample_creates_one_to_five_zones_that_cover_screen_width()
    {
        var field = new NaturalWindField(randomSeed: 7);

        _ = field.Sample(x: 10, screenWidth: 1000, strength: 180, timeSeconds: 0);

        Assert.InRange(field.Zones.Count, 1, 5);
        Assert.Equal(0, field.Zones.First().Left);
        Assert.Equal(1000, field.Zones.Last().Right);
        Assert.All(field.Zones, zone => Assert.True(zone.Right > zone.Left));
    }

    [Fact]
    public void Sample_caps_upward_wind_to_twenty_percent_of_strength()
    {
        var field = new NaturalWindField(randomSeed: 4);

        var wind = field.Sample(x: 120, screenWidth: 1000, strength: 180, timeSeconds: 1.5);

        Assert.InRange(wind.VerticalPixelsPerSecond, -36, 0);
    }

    [Fact]
    public void Sample_varies_wind_by_zone()
    {
        var field = new NaturalWindField(randomSeed: 1, fixedZoneCount: 3);

        var left = field.Sample(x: 80, screenWidth: 900, strength: 180, timeSeconds: 2);
        var right = field.Sample(x: 820, screenWidth: 900, strength: 180, timeSeconds: 2);

        Assert.NotEqual(left.HorizontalPixelsPerSecond, right.HorizontalPixelsPerSecond);
    }
}
