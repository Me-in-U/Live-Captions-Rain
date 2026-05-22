namespace LiveCaptionsRain.Core.Physics;

public sealed record NaturalWindZone(
    double Left,
    double Right,
    double BaseHorizontal,
    double GustHorizontal,
    double VerticalScale,
    double Frequency,
    double Phase,
    double LiftPhase);
