namespace LiveCaptionsRain.Core.Windows;

public static class DesktopWindowPlatformResolver
{
    public static IReadOnlyList<VisibleWindowSurface> Resolve(
        IReadOnlyList<DesktopWindowSnapshot> windowsFromFrontToBack,
        ScreenRect monitorBounds,
        double platformHeight,
        double cornerSize,
        double sideWallProbeWidth = 0,
        double sideWallTopInset = 0)
    {
        var occludingWindows = windowsFromFrontToBack
            .Where(window => DesktopWindowFilter.ShouldOccludeWindow(window, monitorBounds))
            .Select(window => new
            {
                Window = window,
                VisibleBounds = window.Bounds.Intersect(monitorBounds)!.Value
            })
            .ToArray();

        var platformHandles = occludingWindows
            .Where(item => DesktopWindowFilter.ShouldUseWindow(item.Window, monitorBounds))
            .Select(item => item.Window.Handle)
            .ToHashSet();

        return VisibleTopEdgeResolver.Resolve(
                occludingWindows
                .Select(item => new WindowSurface(item.Window.Handle, item.VisibleBounds))
                .ToArray(),
                platformHeight,
                cornerSize,
                sideWallProbeWidth,
                sideWallTopInset)
            .Where(surface => platformHandles.Contains(surface.Handle))
            .ToArray();
    }
}
