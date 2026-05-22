using System;
using System.Collections.Generic;
using System.Linq;
using LiveCaptionsRain.Core.Physics;
using LiveCaptionsRain.Core.Windows;
using LiveCaptionsRain.Native;

namespace LiveCaptionsRain.Services;

internal sealed class DesktopWindowService
{
    private const int CornerSize = 48;
    private readonly int _currentProcessId = Environment.ProcessId;

    public IReadOnlyList<WindowColliderSnapshot> GetWindowColliders(ScreenRect monitorBounds, nint overlayHandle)
    {
        var snapshots = new List<DesktopWindowSnapshot>();

        WindowsApi.EnumWindows((hWnd, _) =>
        {
            if (hWnd == overlayHandle || hWnd == nint.Zero)
            {
                return true;
            }

            WindowsApi.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == _currentProcessId)
            {
                return true;
            }

            if (!TryCreateSnapshot(hWnd, out var snapshot))
            {
                return true;
            }

            snapshots.Add(snapshot);
            return true;
        }, nint.Zero);

        var visibleSurfaces = DesktopWindowPlatformResolver.Resolve(
            snapshots,
            monitorBounds,
            platformHeight: 18,
            cornerSize: CornerSize);

        var byHandle = snapshots.ToDictionary(snapshot => snapshot.Handle);
        return visibleSurfaces
            .Where(surface => surface.TopPlatforms.Count > 0 || surface.LeftCornerVisible || surface.RightCornerVisible)
            .Select(surface =>
            {
                var snapshot = byHandle[surface.Handle];
                var localLeft = snapshot.Bounds.Left - monitorBounds.Left;
                var localTop = snapshot.Bounds.Top - monitorBounds.Top;
                var width = snapshot.Bounds.Width;
                return new WindowColliderSnapshot(
                    surface.Handle,
                    snapshot.Title,
                    surface.TopPlatforms
                        .Select(segment => new PhysicsRect(
                            segment.Left - monitorBounds.Left,
                            segment.Top - monitorBounds.Top,
                            segment.Width,
                            segment.Height))
                        .ToArray(),
                    surface.LeftCornerVisible
                        ? new PhysicsRect(localLeft - CornerSize / 2d, localTop - CornerSize / 2d, CornerSize, CornerSize)
                        : null,
                    surface.RightCornerVisible
                        ? new PhysicsRect(localLeft + width - CornerSize / 2d, localTop - CornerSize / 2d, CornerSize, CornerSize)
                        : null);
            })
            .ToArray();
    }

    private static bool TryCreateSnapshot(nint hWnd, out DesktopWindowSnapshot snapshot)
    {
        snapshot = default!;

        if (!WindowsApi.GetWindowRect(hWnd, out var rect))
        {
            return false;
        }

        var exStyle = WindowsApi.GetWindowLong(hWnd, WindowsApi.GwlExStyle);
        var title = WindowsApi.GetTitle(hWnd);
        var rootOwner = WindowsApi.GetAncestor(hWnd, WindowsApi.GaRootOwner);
        var isCloaked = WindowsApi.IsWindowCloaked(hWnd);

        snapshot = new DesktopWindowSnapshot(
            hWnd,
            title,
            new ScreenRect(rect.Left, rect.Top, rect.Right, rect.Bottom),
            WindowsApi.IsWindowVisible(hWnd) && !isCloaked && !string.IsNullOrWhiteSpace(title),
            WindowsApi.IsIconic(hWnd),
            (exStyle & WindowsApi.WsExToolWindow) != 0,
            rootOwner != nint.Zero && rootOwner != hWnd,
            WindowsApi.IsZoomed(hWnd));

        return true;
    }
}
