using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Tests;

public sealed class DesktopWindowPlatformResolverTests
{
    private static readonly ScreenRect Monitor = new(0, 0, 1920, 1080);

    [Fact]
    public void Resolve_uses_top_attached_front_windows_as_occluders_without_creating_platforms()
    {
        var frontTopAttached = Window(
            handle: 1,
            left: 100,
            top: 0,
            right: 900,
            bottom: 600);
        var hiddenBehind = Window(
            handle: 2,
            left: 300,
            top: 180,
            right: 800,
            bottom: 720);

        var platforms = DesktopWindowPlatformResolver.Resolve(
            [frontTopAttached, hiddenBehind],
            Monitor,
            platformHeight: 18,
            cornerSize: 48);

        Assert.DoesNotContain(platforms, platform => platform.Handle == frontTopAttached.Handle);
        Assert.Empty(platforms.Single(platform => platform.Handle == hiddenBehind.Handle).TopPlatforms);
    }

    [Fact]
    public void Resolve_rejects_windows_outside_selected_monitor()
    {
        var outsideMonitor = Window(
            handle: 1,
            left: 2200,
            top: 160,
            right: 2800,
            bottom: 720);

        var platforms = DesktopWindowPlatformResolver.Resolve(
            [outsideMonitor],
            Monitor,
            platformHeight: 18,
            cornerSize: 48);

        Assert.Empty(platforms);
    }

    [Fact]
    public void ApplyDraggedForeground_recomputes_back_window_visibility_with_dragged_window_on_top()
    {
        var front = Window(
            handle: 1,
            left: 900,
            top: 120,
            right: 1400,
            bottom: 680);
        var back = Window(
            handle: 2,
            left: 300,
            top: 180,
            right: 900,
            bottom: 720);
        var dragged = front with { Bounds = new ScreenRect(260, 150, 940, 680) };

        var reordered = DesktopWindowDragOrderResolver.ApplyDraggedForeground([back, front], dragged);
        var platforms = DesktopWindowPlatformResolver.Resolve(
            reordered,
            Monitor,
            platformHeight: 18,
            cornerSize: 48);

        Assert.Equal(dragged.Handle, reordered[0].Handle);
        Assert.Empty(platforms.Single(platform => platform.Handle == back.Handle).TopPlatforms);
    }

    private static DesktopWindowSnapshot Window(
        nint handle,
        double left,
        double top,
        double right,
        double bottom)
    {
        return new DesktopWindowSnapshot(
            handle,
            "Window",
            new ScreenRect(left, top, right, bottom),
            IsVisible: true,
            IsMinimized: false,
            IsToolWindow: false,
            IsOwnedWindow: false,
            IsMaximized: false);
    }
}
