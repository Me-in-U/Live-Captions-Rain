using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Services;

public sealed record MonitorInfo(string DeviceName, string DisplayName, ScreenRect Bounds, bool IsPrimary)
{
    public override string ToString() => DisplayName;
}
