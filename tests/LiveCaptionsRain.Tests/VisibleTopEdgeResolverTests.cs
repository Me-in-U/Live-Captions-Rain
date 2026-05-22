using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Tests;

public sealed class VisibleTopEdgeResolverTests
{
    [Fact]
    public void Resolve_keeps_only_visible_top_edge_segments_for_windows_behind_front_windows()
    {
        var front = WindowSurface.Create(handle: 1, left: 280, top: 100, right: 360, bottom: 420);
        var frontMiddle = WindowSurface.Create(handle: 3, left: 500, top: 100, right: 600, bottom: 420);
        var back = WindowSurface.Create(handle: 2, left: 300, top: 150, right: 800, bottom: 520);

        var surfaces = VisibleTopEdgeResolver.Resolve([front, frontMiddle, back], platformHeight: 18, cornerSize: 48);

        var backSurface = surfaces.Single(surface => surface.Handle == 2);
        Assert.Equal(2, backSurface.TopPlatforms.Count);
        Assert.Equal(new VisibleEdgeSegment(360, 150, 140, 18), backSurface.TopPlatforms[0]);
        Assert.Equal(new VisibleEdgeSegment(600, 150, 200, 18), backSurface.TopPlatforms[1]);
        Assert.False(backSurface.LeftCornerVisible);
        Assert.True(backSurface.RightCornerVisible);
    }

    [Fact]
    public void Resolve_removes_fully_occluded_back_window_top_edge()
    {
        var front = WindowSurface.Create(handle: 1, left: 100, top: 80, right: 900, bottom: 500);
        var back = WindowSurface.Create(handle: 2, left: 300, top: 150, right: 800, bottom: 520);

        var surfaces = VisibleTopEdgeResolver.Resolve([front, back], platformHeight: 18, cornerSize: 48);

        var backSurface = surfaces.Single(surface => surface.Handle == 2);
        Assert.Empty(backSurface.TopPlatforms);
        Assert.False(backSurface.LeftCornerVisible);
        Assert.False(backSurface.RightCornerVisible);
    }

    [Fact]
    public void Resolve_does_not_let_lower_window_hide_higher_window()
    {
        var front = WindowSurface.Create(handle: 1, left: 100, top: 100, right: 500, bottom: 420);
        var back = WindowSurface.Create(handle: 2, left: 0, top: 50, right: 900, bottom: 520);

        var surfaces = VisibleTopEdgeResolver.Resolve([front, back], platformHeight: 18, cornerSize: 48);

        var frontSurface = surfaces.Single(surface => surface.Handle == 1);
        Assert.Equal([new VisibleEdgeSegment(100, 100, 400, 18)], frontSurface.TopPlatforms);
        Assert.True(frontSurface.LeftCornerVisible);
        Assert.True(frontSurface.RightCornerVisible);
    }

    [Fact]
    public void Resolve_keeps_only_visible_side_edge_segments_for_windows_behind_front_windows()
    {
        var front = WindowSurface.Create(handle: 1, left: 70, top: 100, right: 280, bottom: 260);
        var back = WindowSurface.Create(handle: 2, left: 100, top: 80, right: 500, bottom: 400);

        var surfaces = VisibleTopEdgeResolver.Resolve(
            [front, back],
            platformHeight: 18,
            cornerSize: 48,
            sideWallProbeWidth: 384,
            sideWallTopInset: 36);

        var backSurface = surfaces.Single(surface => surface.Handle == 2);
        Assert.Equal([new VisibleEdgeSegment(-92, 260, 384, 140)], backSurface.LeftSideSegments);
        Assert.Equal([new VisibleEdgeSegment(308, 116, 384, 284)], backSurface.RightSideSegments);
    }
}
