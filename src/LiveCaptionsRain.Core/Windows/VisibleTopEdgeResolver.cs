namespace LiveCaptionsRain.Core.Windows;

public static class VisibleTopEdgeResolver
{
    public static IReadOnlyList<VisibleWindowSurface> Resolve(
        IReadOnlyList<WindowSurface> windowsFromFrontToBack,
        double platformHeight,
        double cornerSize,
        double sideWallProbeWidth = 0,
        double sideWallTopInset = 0)
    {
        var frontWindows = new List<ScreenRect>();
        var result = new List<VisibleWindowSurface>();

        foreach (var window in windowsFromFrontToBack)
        {
            var visibleSegments = ResolveVisibleTopSegments(window.Bounds, frontWindows, platformHeight);
            result.Add(new VisibleWindowSurface(
                window.Handle,
                window.Bounds,
                visibleSegments,
                IsCornerVisible(window.Bounds.Left, window.Bounds.Top, cornerSize, frontWindows),
                IsCornerVisible(window.Bounds.Right, window.Bounds.Top, cornerSize, frontWindows),
                ResolveVisibleSideSegments(window.Bounds, frontWindows, sideWallProbeWidth, sideWallTopInset, isRightSide: false),
                ResolveVisibleSideSegments(window.Bounds, frontWindows, sideWallProbeWidth, sideWallTopInset, isRightSide: true)));

            frontWindows.Add(window.Bounds);
        }

        return result;
    }

    private static IReadOnlyList<VisibleEdgeSegment> ResolveVisibleTopSegments(
        ScreenRect bounds,
        IReadOnlyList<ScreenRect> occluders,
        double platformHeight)
    {
        var segments = new List<(double Left, double Right)> { (bounds.Left, bounds.Right) };
        var platformBottom = bounds.Top + platformHeight;

        foreach (var occluder in occluders)
        {
            if (!OverlapsVertical(occluder, bounds.Top, platformBottom))
            {
                continue;
            }

            var cutLeft = Math.Max(bounds.Left, occluder.Left);
            var cutRight = Math.Min(bounds.Right, occluder.Right);
            if (cutRight <= cutLeft)
            {
                continue;
            }

            segments = segments
                .SelectMany(segment => Subtract(segment.Left, segment.Right, cutLeft, cutRight))
                .ToList();
        }

        return segments
            .Where(segment => segment.Right - segment.Left >= 8)
            .Select(segment => new VisibleEdgeSegment(segment.Left, bounds.Top, segment.Right - segment.Left, platformHeight))
            .ToArray();
    }

    private static bool IsCornerVisible(double x, double y, double cornerSize, IReadOnlyList<ScreenRect> occluders)
    {
        var half = cornerSize / 2d;
        var left = x - half;
        var right = x + half;
        var top = y - half;
        var bottom = y + half;

        return !occluders.Any(occluder =>
            occluder.Left < right
            && occluder.Right > left
            && occluder.Top < bottom
            && occluder.Bottom > top);
    }

    private static IReadOnlyList<VisibleEdgeSegment> ResolveVisibleSideSegments(
        ScreenRect bounds,
        IReadOnlyList<ScreenRect> occluders,
        double sideWallProbeWidth,
        double sideWallTopInset,
        bool isRightSide)
    {
        if (sideWallProbeWidth <= 0 || bounds.Height <= sideWallTopInset)
        {
            return [];
        }

        var halfWidth = sideWallProbeWidth / 2d;
        var sideLeft = (isRightSide ? bounds.Right : bounds.Left) - halfWidth;
        var sideRight = sideLeft + sideWallProbeWidth;
        var visibleTop = bounds.Top + Math.Max(0, sideWallTopInset);
        var segments = new List<(double Top, double Bottom)> { (visibleTop, bounds.Bottom) };

        foreach (var occluder in occluders)
        {
            if (!OverlapsHorizontal(occluder, sideLeft, sideRight))
            {
                continue;
            }

            var cutTop = Math.Max(visibleTop, occluder.Top);
            var cutBottom = Math.Min(bounds.Bottom, occluder.Bottom);
            if (cutBottom <= cutTop)
            {
                continue;
            }

            segments = segments
                .SelectMany(segment => Subtract(segment.Top, segment.Bottom, cutTop, cutBottom))
                .ToList();
        }

        return segments
            .Where(segment => segment.Bottom - segment.Top >= 8)
            .Select(segment => new VisibleEdgeSegment(sideLeft, segment.Top, sideWallProbeWidth, segment.Bottom - segment.Top))
            .ToArray();
    }

    private static bool OverlapsVertical(ScreenRect rect, double top, double bottom)
    {
        return rect.Top < bottom && rect.Bottom > top;
    }

    private static bool OverlapsHorizontal(ScreenRect rect, double left, double right)
    {
        return rect.Left < right && rect.Right > left;
    }

    private static IEnumerable<(double Left, double Right)> Subtract(
        double left,
        double right,
        double cutLeft,
        double cutRight)
    {
        if (cutLeft > left)
        {
            yield return (left, cutLeft);
        }

        if (cutRight < right)
        {
            yield return (cutRight, right);
        }
    }
}
