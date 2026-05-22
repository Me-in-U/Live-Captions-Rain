namespace LiveCaptionsRain.Core.Windows;

public readonly record struct ScreenRect(double Left, double Top, double Right, double Bottom)
{
    public double Width => Math.Max(0, Right - Left);

    public double Height => Math.Max(0, Bottom - Top);

    public bool NearlyEquals(ScreenRect other, double tolerance = 1)
    {
        return Math.Abs(Left - other.Left) <= tolerance
            && Math.Abs(Top - other.Top) <= tolerance
            && Math.Abs(Right - other.Right) <= tolerance
            && Math.Abs(Bottom - other.Bottom) <= tolerance;
    }

    public bool Intersects(ScreenRect other)
    {
        return Left < other.Right
            && Right > other.Left
            && Top < other.Bottom
            && Bottom > other.Top;
    }

    public ScreenRect? Intersect(ScreenRect other)
    {
        if (!Intersects(other))
        {
            return null;
        }

        return new ScreenRect(
            Math.Max(Left, other.Left),
            Math.Max(Top, other.Top),
            Math.Min(Right, other.Right),
            Math.Min(Bottom, other.Bottom));
    }
}
