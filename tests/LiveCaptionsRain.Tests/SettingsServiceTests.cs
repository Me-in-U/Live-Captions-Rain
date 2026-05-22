using LiveCaptionsRain.Core.Settings;

namespace LiveCaptionsRain.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public void Defaults_match_plan_cleanup_and_clickthrough()
    {
        var settings = LiveCaptionsRainSettings.CreateDefault();

        Assert.Equal(60, settings.CleanupLifetimeSeconds);
        Assert.Equal(200, settings.MaxActiveWords);
        Assert.False(settings.IsRunning);
        Assert.True(settings.ClickThrough);
        Assert.False(settings.InteractionMode);
        Assert.True(settings.StackOnWindows);
        Assert.True(settings.FractureOnWordPiles);
        Assert.False(settings.WindowSideWalls);
        Assert.Equal(SpawnMode.Random, settings.SpawnMode);
        Assert.Equal(180, settings.RandomWindStrength);
        Assert.Equal(22, settings.FontSize);
        Assert.Equal(700, settings.CaptionDelayMilliseconds);
        Assert.Equal(0, settings.StrokeThickness);
    }

    [Fact]
    public void Settings_do_not_expose_spawn_rate_throttle()
    {
        Assert.Null(typeof(LiveCaptionsRainSettings).GetProperty("SpawnRatePerSecond"));
    }

    [Fact]
    public async Task LoadAsync_returns_defaults_when_file_is_missing()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json");
        var store = new SettingsStore(path);

        var settings = await store.LoadAsync();

        Assert.Equal(60, settings.CleanupLifetimeSeconds);
        Assert.Equal(200, settings.MaxActiveWords);
    }

    [Fact]
    public async Task SaveAsync_and_LoadAsync_round_trip_user_values()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new SettingsStore(Path.Combine(folder, "settings.json"));
        var settings = LiveCaptionsRainSettings.CreateDefault() with
        {
            FontFamily = "Malgun Gothic",
            FontSize = 52,
            SpawnMode = SpawnMode.LeftToRight,
            StackOnWindows = false,
            FractureOnWordPiles = false,
            WindowSideWalls = true,
            CaptionDelayMilliseconds = 1200,
            CleanupLifetimeSeconds = 12,
            MaxActiveWords = 42
        };

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(settings.FontFamily, loaded.FontFamily);
        Assert.Equal(settings.FontSize, loaded.FontSize);
        Assert.Equal(settings.SpawnMode, loaded.SpawnMode);
        Assert.Equal(settings.StackOnWindows, loaded.StackOnWindows);
        Assert.Equal(settings.FractureOnWordPiles, loaded.FractureOnWordPiles);
        Assert.Equal(settings.WindowSideWalls, loaded.WindowSideWalls);
        Assert.Equal(settings.CaptionDelayMilliseconds, loaded.CaptionDelayMilliseconds);
        Assert.Equal(settings.CleanupLifetimeSeconds, loaded.CleanupLifetimeSeconds);
        Assert.Equal(settings.MaxActiveWords, loaded.MaxActiveWords);
    }
}
