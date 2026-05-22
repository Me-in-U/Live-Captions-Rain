using LiveCaptionsRain.Core.Physics;
using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Tests;

public sealed class WindowCollisionRectBuilderTests
{
    [Fact]
    public void BuildCollisionRects_returns_top_platforms_only_when_side_walls_are_disabled()
    {
        var surface = Surface();

        var rects = WindowCollisionRectBuilder.BuildCollisionRects(
            surface,
            new ScreenRect(100, 50, 1000, 800),
            includeSideWalls: false,
            topThickness: 36,
            sideThickness: 384);

        var rect = Assert.Single(rects);
        Assert.Equal(new PhysicsRect(100, 150, 300, 36), rect);
    }

    [Fact]
    public void BuildCollisionRects_adds_left_and_right_side_walls_when_enabled()
    {
        var surface = Surface();

        var rects = WindowCollisionRectBuilder.BuildCollisionRects(
            surface,
            new ScreenRect(100, 50, 1000, 800),
            includeSideWalls: true,
            topThickness: 36,
            sideThickness: 384);

        Assert.Equal(
            [
                new PhysicsRect(100, 150, 300, 36),
                new PhysicsRect(-92, 186, 384, 204),
                new PhysicsRect(308, 186, 384, 204)
            ],
            rects);
    }

    private static VisibleWindowSurface Surface()
    {
        return new VisibleWindowSurface(
            new nint(1),
            new ScreenRect(200, 200, 500, 440),
            [new VisibleEdgeSegment(200, 200, 300, 18)],
            LeftCornerVisible: true,
            RightCornerVisible: true,
            [new VisibleEdgeSegment(8, 236, 384, 204)],
            [new VisibleEdgeSegment(408, 236, 384, 204)]);
    }
}
