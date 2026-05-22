namespace LiveCaptionsRain.Core.Windows;

public sealed record DesktopWindowSnapshot(
    nint Handle,
    string Title,
    ScreenRect Bounds,
    bool IsVisible,
    bool IsMinimized,
    bool IsToolWindow,
    bool IsOwnedWindow,
    bool IsMaximized);
