namespace LiveCaptionsRain.Core.Physics;

public static class PhysicsRectSetComparer
{
    public static bool AreEquivalent(
        IEnumerable<PhysicsRect> current,
        IEnumerable<PhysicsRect> next,
        double tolerance)
    {
        var currentItems = Normalize(current).ToArray();
        var nextItems = Normalize(next).ToArray();
        if (currentItems.Length != nextItems.Length)
        {
            return false;
        }

        for (var index = 0; index < currentItems.Length; index++)
        {
            if (!NearlyEquals(currentItems[index], nextItems[index], tolerance))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<PhysicsRect> Normalize(IEnumerable<PhysicsRect> rects)
    {
        return rects
            .OrderBy(rect => rect.Left)
            .ThenBy(rect => rect.Top)
            .ThenBy(rect => rect.Width)
            .ThenBy(rect => rect.Height);
    }

    private static bool NearlyEquals(PhysicsRect left, PhysicsRect right, double tolerance)
    {
        return Math.Abs(left.Left - right.Left) <= tolerance
            && Math.Abs(left.Top - right.Top) <= tolerance
            && Math.Abs(left.Width - right.Width) <= tolerance
            && Math.Abs(left.Height - right.Height) <= tolerance;
    }
}
