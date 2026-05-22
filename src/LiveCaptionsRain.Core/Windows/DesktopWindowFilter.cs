namespace LiveCaptionsRain.Core.Windows;

public static class DesktopWindowFilter
{
    public static bool ShouldOccludeWindow(DesktopWindowSnapshot window, ScreenRect monitorBounds)
    {
        return window.IsVisible
            && !window.IsMinimized
            && !window.IsToolWindow
            && !window.IsOwnedWindow
            && window.Bounds.Width >= 48
            && window.Bounds.Height >= 48
            && window.Bounds.Intersects(monitorBounds);
    }

    public static bool ShouldUseWindow(DesktopWindowSnapshot window, ScreenRect monitorBounds)
    {
        if (!ShouldOccludeWindow(window, monitorBounds)
            || window.IsMaximized
            || window.Bounds.NearlyEquals(monitorBounds))
        {
            return false;
        }

        var visibleBounds = window.Bounds.Intersect(monitorBounds);
        if (visibleBounds is null)
        {
            return false;
        }

        return visibleBounds.Value.Top > monitorBounds.Top + 1;
    }
}
