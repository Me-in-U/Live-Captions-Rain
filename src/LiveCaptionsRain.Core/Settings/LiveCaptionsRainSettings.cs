namespace LiveCaptionsRain.Core.Settings;

public sealed record LiveCaptionsRainSettings
{
    public bool IsRunning { get; init; }

    public bool ClickThrough { get; init; } = true;

    public bool InteractionMode { get; init; }

    public bool WindowCollision { get; init; } = true;

    public bool StackOnWindows { get; init; } = true;

    public bool FractureOnWordPiles { get; init; } = true;

    public bool WindowSideWalls { get; init; }

    public bool RandomWind { get; init; } = true;

    public bool UseAllMonitors { get; init; }

    public string? MonitorDeviceName { get; init; }

    public string FontFamily { get; init; } = "Malgun Gothic";

    public double FontSize { get; init; } = 22;

    public string FontColor { get; init; } = "#FFFFFFFF";

    public bool UseFill { get; init; } = true;

    public string FillColor { get; init; } = "#FFFFFFFF";

    public string OutlineColor { get; init; } = "#FF111827";

    public double StrokeThickness { get; init; }

    public string FontWeight { get; init; } = "SemiBold";

    public double Opacity { get; init; } = 0.96;

    public bool Shadow { get; init; } = true;

    public SpawnMode SpawnMode { get; init; } = SpawnMode.Random;

    public double RandomWindStrength { get; init; } = 180;

    public int CaptionDelayMilliseconds { get; init; } = 700;

    public int CleanupLifetimeSeconds { get; init; } = 60;

    public int MaxActiveWords { get; init; } = 200;

    public static LiveCaptionsRainSettings CreateDefault() => new();

    public LiveCaptionsRainSettings Sanitize()
    {
        return this with
        {
            FontSize = Math.Clamp(FontSize, 10, 160),
            StrokeThickness = Math.Clamp(StrokeThickness, 0, 12),
            Opacity = Math.Clamp(Opacity, 0.1, 1),
            RandomWindStrength = Math.Clamp(RandomWindStrength, 0, 600),
            CaptionDelayMilliseconds = Math.Clamp(CaptionDelayMilliseconds, 0, 5000),
            CleanupLifetimeSeconds = Math.Clamp(CleanupLifetimeSeconds, 5, 3600),
            MaxActiveWords = Math.Clamp(MaxActiveWords, 10, 2000)
        };
    }
}
