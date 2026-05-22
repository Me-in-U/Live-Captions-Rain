namespace LiveCaptionsRain.Core.Physics;

public sealed record PhysicsWordSnapshot(
    Guid Id,
    string Text,
    PhysicsRect Bounds,
    double AngleRadians,
    DateTimeOffset CreatedAt,
    double Opacity = 1,
    double FallDistancePixels = 0,
    bool HighFallFractureEnabled = true,
    bool IsDeleting = false,
    DateTimeOffset? DeletionStartedAt = null,
    DateTimeOffset? DeleteAt = null);
