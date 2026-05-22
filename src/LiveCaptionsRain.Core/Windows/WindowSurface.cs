namespace LiveCaptionsRain.Core.Windows;

public sealed record WindowSurface(nint Handle, ScreenRect Bounds)
{
    public static WindowSurface Create(nint handle, double left, double top, double right, double bottom)
    {
        return new WindowSurface(handle, new ScreenRect(left, top, right, bottom));
    }
}
