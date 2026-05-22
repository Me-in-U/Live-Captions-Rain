using System.Collections.Generic;
using LiveCaptionsRain.Core.Physics;

namespace LiveCaptionsRain.Services;

internal sealed record WindowColliderSnapshot(
    nint Handle,
    string Title,
    IReadOnlyList<PhysicsRect> TopPlatforms,
    PhysicsRect? LeftCorner,
    PhysicsRect? RightCorner);
