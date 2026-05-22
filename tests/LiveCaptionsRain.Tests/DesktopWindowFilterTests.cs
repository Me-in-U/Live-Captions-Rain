using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Tests;

public sealed class DesktopWindowFilterTests
{
    private static readonly ScreenRect Monitor = new(0, 0, 1920, 1080);

    [Fact]
    public void ShouldUseWindow_accepts_normal_visible_window_with_top_gap()
    {
        var window = new DesktopWindowSnapshot(
            Handle: 10,
            Title: "Editor",
            Bounds: new ScreenRect(300, 120, 900, 620),
            IsVisible: true,
            IsMinimized: false,
            IsToolWindow: false,
            IsOwnedWindow: false,
            IsMaximized: false);

        Assert.True(DesktopWindowFilter.ShouldUseWindow(window, Monitor));
    }

    [Theory]
    [InlineData(false, false, false, false, false, 120)]
    [InlineData(true, true, false, false, false, 120)]
    [InlineData(true, false, true, false, false, 120)]
    [InlineData(true, false, false, true, false, 120)]
    [InlineData(true, false, false, false, true, 120)]
    [InlineData(true, false, false, false, false, 0)]
    [InlineData(true, false, false, false, false, -10)]
    public void ShouldUseWindow_rejects_non_interactive_or_no_gap_windows(
        bool visible,
        bool minimized,
        bool toolWindow,
        bool ownedWindow,
        bool maximized,
        double top)
    {
        var window = new DesktopWindowSnapshot(
            Handle: 10,
            Title: "Blocked",
            Bounds: new ScreenRect(100, top, 900, top + 500),
            IsVisible: visible,
            IsMinimized: minimized,
            IsToolWindow: toolWindow,
            IsOwnedWindow: ownedWindow,
            IsMaximized: maximized);

        Assert.False(DesktopWindowFilter.ShouldUseWindow(window, Monitor));
    }

    [Fact]
    public void ShouldUseWindow_rejects_fullscreen_bounds()
    {
        var window = new DesktopWindowSnapshot(
            Handle: 10,
            Title: "Fullscreen",
            Bounds: Monitor,
            IsVisible: true,
            IsMinimized: false,
            IsToolWindow: false,
            IsOwnedWindow: false,
            IsMaximized: false);

        Assert.False(DesktopWindowFilter.ShouldUseWindow(window, Monitor));
    }
}
