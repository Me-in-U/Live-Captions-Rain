namespace LiveCaptionsRain.Core.Physics;

public sealed class NaturalWindField
{
    private const int MaxZones = 5;
    private const double VerticalLiftRatio = 0.2d;

    private readonly Random _random;
    private readonly int? _fixedZoneCount;
    private IReadOnlyList<NaturalWindZone> _zones = [];
    private double _screenWidth;
    private double _nextReseedAtSeconds;

    public NaturalWindField(int? randomSeed = null, int? fixedZoneCount = null)
    {
        _random = randomSeed is null ? new Random() : new Random(randomSeed.Value);
        _fixedZoneCount = fixedZoneCount is null ? null : Math.Clamp(fixedZoneCount.Value, 1, MaxZones);
    }

    public IReadOnlyList<NaturalWindZone> Zones => _zones;

    public WindVector Sample(double x, double screenWidth, double strength, double timeSeconds)
    {
        var width = Math.Max(1, screenWidth);
        var normalizedStrength = Math.Max(0, strength);
        EnsureZones(width, timeSeconds);

        var zone = FindZone(Math.Clamp(x, 0, width));
        var gust =
            Math.Sin(timeSeconds * zone.Frequency + zone.Phase) * 0.34d
            + Math.Sin(timeSeconds * zone.Frequency * 0.41d + zone.LiftPhase) * 0.16d;
        var horizontal = (zone.BaseHorizontal + zone.GustHorizontal * gust) * normalizedStrength;

        var liftPulse = 0.35d + 0.65d * ((Math.Sin(timeSeconds * zone.Frequency * 0.63d + zone.LiftPhase) + 1d) / 2d);
        var vertical = -normalizedStrength * VerticalLiftRatio * zone.VerticalScale * liftPulse;

        return new WindVector(horizontal, vertical);
    }

    public void Reset()
    {
        _zones = [];
        _screenWidth = 0;
        _nextReseedAtSeconds = 0;
    }

    private void EnsureZones(double screenWidth, double timeSeconds)
    {
        if (_zones.Count > 0
            && Math.Abs(_screenWidth - screenWidth) < 0.5d
            && timeSeconds < _nextReseedAtSeconds)
        {
            return;
        }

        _screenWidth = screenWidth;
        _nextReseedAtSeconds = timeSeconds + 10d + _random.NextDouble() * 12d;
        _zones = CreateZones(screenWidth);
    }

    private IReadOnlyList<NaturalWindZone> CreateZones(double screenWidth)
    {
        var zoneCount = _fixedZoneCount ?? _random.Next(1, MaxZones + 1);
        var weights = Enumerable.Range(0, zoneCount)
            .Select(_ => 0.65d + _random.NextDouble() * 0.9d)
            .ToArray();
        var totalWeight = weights.Sum();

        var zones = new List<NaturalWindZone>(zoneCount);
        var left = 0d;
        for (var index = 0; index < zoneCount; index++)
        {
            var right = index == zoneCount - 1
                ? screenWidth
                : left + screenWidth * weights[index] / totalWeight;
            var baseHorizontal = RandomSignedMagnitude(0.28d, 0.95d);
            var gustHorizontal = RandomSignedMagnitude(0.12d, 0.45d);
            zones.Add(new NaturalWindZone(
                left,
                right,
                baseHorizontal,
                gustHorizontal,
                VerticalScale: 0.35d + _random.NextDouble() * 0.65d,
                Frequency: 0.35d + _random.NextDouble() * 0.85d,
                Phase: _random.NextDouble() * Math.Tau,
                LiftPhase: _random.NextDouble() * Math.Tau));
            left = right;
        }

        return zones;
    }

    private NaturalWindZone FindZone(double x)
    {
        return _zones.FirstOrDefault(zone => x >= zone.Left && x < zone.Right)
            ?? _zones[^1];
    }

    private double RandomSignedMagnitude(double min, double max)
    {
        var sign = _random.Next(0, 2) == 0 ? -1d : 1d;
        return sign * (min + _random.NextDouble() * (max - min));
    }
}
