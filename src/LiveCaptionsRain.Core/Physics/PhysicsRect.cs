namespace LiveCaptionsRain.Core.Physics;

public readonly record struct PhysicsRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;

    public double Bottom => Top + Height;

    public double CenterX => Left + Width / 2d;

    public double CenterY => Top + Height / 2d;

    public bool Contains(double x, double y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

    public bool Intersects(PhysicsRect other)
    {
        return other.Left < Right
            && other.Right > Left
            && other.Top < Bottom
            && other.Bottom > Top;
    }
}
