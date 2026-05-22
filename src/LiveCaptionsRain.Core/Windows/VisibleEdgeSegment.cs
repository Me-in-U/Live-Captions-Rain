namespace LiveCaptionsRain.Core.Windows;

public readonly record struct VisibleEdgeSegment(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
}
