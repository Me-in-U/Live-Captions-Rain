using LiveCaptionsRain.Core.Physics;
using LiveCaptionsRain.Core.Settings;
using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Tests;

public sealed class SpawnPlannerTests
{
    private static readonly ScreenRect Area = new(0, 0, 1000, 700);

    [Fact]
    public void NextX_walks_left_to_right()
    {
        var planner = new SpawnPlanner(randomSeed: 1);

        Assert.Equal(50, planner.NextX(SpawnMode.LeftToRight, Area, 100));
        Assert.Equal(175, planner.NextX(SpawnMode.LeftToRight, Area, 100));
        Assert.Equal(300, planner.NextX(SpawnMode.LeftToRight, Area, 100));
    }

    [Fact]
    public void NextX_walks_right_to_left()
    {
        var planner = new SpawnPlanner(randomSeed: 1);

        Assert.Equal(950, planner.NextX(SpawnMode.RightToLeft, Area, 100));
        Assert.Equal(825, planner.NextX(SpawnMode.RightToLeft, Area, 100));
    }

    [Fact]
    public void NextX_keeps_center_biased_values_inside_bounds()
    {
        var planner = new SpawnPlanner(randomSeed: 1);

        var values = Enumerable.Range(0, 50)
            .Select(_ => planner.NextX(SpawnMode.CenterBiased, Area, 160))
            .ToArray();

        Assert.All(values, value => Assert.InRange(value, 80, 920));
    }
}
