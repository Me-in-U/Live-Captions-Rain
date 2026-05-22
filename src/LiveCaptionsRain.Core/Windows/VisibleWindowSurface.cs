namespace LiveCaptionsRain.Core.Windows;

public sealed record VisibleWindowSurface(
    nint Handle,
    ScreenRect Bounds,
    IReadOnlyList<VisibleEdgeSegment> TopPlatforms,
    bool LeftCornerVisible,
    bool RightCornerVisible);
