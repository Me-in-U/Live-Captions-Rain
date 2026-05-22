using LiveCaptionsRain.Core.Physics;

namespace LiveCaptionsRain.Core.Windows;

public static class WindowCollisionRectBuilder
{
    public static IReadOnlyList<PhysicsRect> BuildCollisionRects(
        VisibleWindowSurface surface,
        ScreenRect monitorBounds,
        bool includeSideWalls,
        double topThickness,
        double sideThickness)
    {
        var rects = BuildTopCollisionRects(surface, monitorBounds, topThickness).ToList();
        if (includeSideWalls)
        {
            rects.AddRange(BuildSideWallRects(surface, monitorBounds, sideThickness));
        }

        return rects;
    }

    public static IReadOnlyList<PhysicsRect> BuildTopCollisionRects(
        VisibleWindowSurface surface,
        ScreenRect monitorBounds,
        double topThickness)
    {
        return surface.TopPlatforms
            .Select(segment => new PhysicsRect(
                segment.Left - monitorBounds.Left,
                segment.Top - monitorBounds.Top,
                segment.Width,
                topThickness))
            .ToArray();
    }

    public static IReadOnlyList<PhysicsRect> BuildSideWallRects(
        VisibleWindowSurface surface,
        ScreenRect monitorBounds,
        double sideThickness)
    {
        return surface.LeftSideSegments
            .Concat(surface.RightSideSegments)
            .Where(segment => segment.Height >= 1)
            .Select(segment => new PhysicsRect(
                segment.Left - monitorBounds.Left,
                segment.Top - monitorBounds.Top,
                sideThickness,
                segment.Height))
            .ToArray();
    }
}
