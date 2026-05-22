using System.Collections.Generic;
using LiveCaptionsRain.Core.Windows;

namespace LiveCaptionsRain.Services;

internal sealed record WindowCollisionState(
    IReadOnlyList<DesktopWindowSnapshot> Snapshots,
    IReadOnlyList<WindowColliderSnapshot> Colliders);
