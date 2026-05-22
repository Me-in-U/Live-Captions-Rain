using LiveCaptionsRain.Core.Timing;

namespace LiveCaptionsRain.Tests;

public sealed class FrameCadenceTests
{
    [Fact]
    public void ShouldRun_treats_min_value_as_forced_refresh_without_overflow()
    {
        Assert.True(FrameCadence.ShouldRun(
            TimeSpan.FromSeconds(1),
            TimeSpan.MinValue,
            TimeSpan.FromMilliseconds(260)));
    }

    [Fact]
    public void ShouldRun_treats_clock_rewind_as_forced_refresh()
    {
        Assert.True(FrameCadence.ShouldRun(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMilliseconds(260)));
    }

    [Fact]
    public void ShouldRun_waits_until_interval_elapses()
    {
        Assert.False(FrameCadence.ShouldRun(
            TimeSpan.FromMilliseconds(1000),
            TimeSpan.FromMilliseconds(900),
            TimeSpan.FromMilliseconds(260)));
        Assert.True(FrameCadence.ShouldRun(
            TimeSpan.FromMilliseconds(1200),
            TimeSpan.FromMilliseconds(900),
            TimeSpan.FromMilliseconds(260)));
    }
}
