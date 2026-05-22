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
    private const int PlatformHeight = 18;
    private readonly int _currentProcessId = Environment.ProcessId;

    public WindowCollisionState GetWindowCollisionState(ScreenRect monitorBounds, nint overlayHandle)
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

        var snapshotArray = snapshots.ToArray();
        return new WindowCollisionState(
            snapshotArray,
            ResolveWindowColliders(snapshotArray, monitorBounds));
    }

    public IReadOnlyList<WindowColliderSnapshot> GetWindowColliders(ScreenRect monitorBounds, nint overlayHandle)
    {
        return GetWindowCollisionState(monitorBounds, overlayHandle).Colliders;
    }

    public IReadOnlyList<WindowColliderSnapshot> ResolveWindowColliders(
        IReadOnlyList<DesktopWindowSnapshot> snapshots,
        ScreenRect monitorBounds)
    {
        var visibleSurfaces = DesktopWindowPlatformResolver.Resolve(
            snapshots,
            monitorBounds,
            platformHeight: PlatformHeight,
            cornerSize: CornerSize);

        var byHandle = snapshots.ToDictionary(snapshot => snapshot.Handle);
        return visibleSurfaces
            .Where(surface => surface.TopPlatforms.Count > 0 || surface.LeftCornerVisible || surface.RightCornerVisible)
            .Select(surface => CreateCollider(byHandle[surface.Handle], surface, monitorBounds))
            .ToArray();
    }

    public bool TryGetWindowSnapshot(ScreenRect monitorBounds, nint overlayHandle, nint hWnd, out DesktopWindowSnapshot snapshot)
    {
        snapshot = default!;
        if (hWnd == overlayHandle || hWnd == nint.Zero)
        {
            return false;
        }

        WindowsApi.GetWindowThreadProcessId(hWnd, out var processId);
        if (processId == _currentProcessId || !TryCreateSnapshot(hWnd, out snapshot))
        {
            return false;
        }

        return DesktopWindowFilter.ShouldOccludeWindow(snapshot, monitorBounds);
    }

    public bool TryGetWindowCollider(ScreenRect monitorBounds, nint overlayHandle, nint hWnd, out WindowColliderSnapshot collider)
    {
        collider = default!;
        if (!TryGetWindowSnapshot(monitorBounds, overlayHandle, hWnd, out var snapshot)
            || !DesktopWindowFilter.ShouldUseWindow(snapshot, monitorBounds))
        {
            return false;
        }

        var visibleBounds = snapshot.Bounds.Intersect(monitorBounds);
        if (visibleBounds is null)
        {
            return false;
        }

        var surface = VisibleTopEdgeResolver.Resolve(
            [new WindowSurface(snapshot.Handle, visibleBounds.Value)],
            PlatformHeight,
            CornerSize).Single();
        if (surface.TopPlatforms.Count == 0 && !surface.LeftCornerVisible && !surface.RightCornerVisible)
        {
            return false;
        }

        collider = CreateCollider(snapshot, surface, monitorBounds);
        return true;
    }

    private static bool TryCreateSnapshot(nint hWnd, out DesktopWindowSnapshot snapshot)
    {
        snapshot = default!;

        if (!WindowsApi.GetWindowRect(hWnd, out var rect))
        {
            return false;
        }

        var bounds = WindowsApi.TryGetExtendedFrameBounds(hWnd, out var visibleRect)
            ? visibleRect
            : rect;
        var exStyle = WindowsApi.GetWindowLong(hWnd, WindowsApi.GwlExStyle);
        var title = WindowsApi.GetTitle(hWnd);
        var rootOwner = WindowsApi.GetAncestor(hWnd, WindowsApi.GaRootOwner);
        var isCloaked = WindowsApi.IsWindowCloaked(hWnd);

        snapshot = new DesktopWindowSnapshot(
            hWnd,
            title,
            new ScreenRect(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom),
            WindowsApi.IsWindowVisible(hWnd) && !isCloaked && !string.IsNullOrWhiteSpace(title),
            WindowsApi.IsIconic(hWnd),
            (exStyle & WindowsApi.WsExToolWindow) != 0,
            rootOwner != nint.Zero && rootOwner != hWnd,
            WindowsApi.IsZoomed(hWnd));

        return true;
    }

    private static WindowColliderSnapshot CreateCollider(
        DesktopWindowSnapshot snapshot,
        VisibleWindowSurface surface,
        ScreenRect monitorBounds)
    {
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
    }
}
