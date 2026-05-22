using System.Collections.Generic;
using System.Linq;
using LiveCaptionsRain.Core.Windows;
using Forms = System.Windows.Forms;

namespace LiveCaptionsRain.Services;

public sealed class MonitorService
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return Forms.Screen.AllScreens
            .Select((screen, index) => new MonitorInfo(
                screen.DeviceName,
                $"{(screen.Primary ? "Primary" : "Monitor")} {index + 1} ({screen.Bounds.Width}x{screen.Bounds.Height})",
                new ScreenRect(screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Right, screen.Bounds.Bottom),
                screen.Primary))
            .ToArray();
    }

    public MonitorInfo Resolve(string? deviceName)
    {
        var monitors = GetMonitors();
        return monitors.FirstOrDefault(item => item.DeviceName == deviceName)
            ?? monitors.FirstOrDefault(item => item.IsPrimary)
            ?? monitors.First();
    }

    public ScreenRect GetVirtualBounds()
    {
        var screen = Forms.SystemInformation.VirtualScreen;
        return new ScreenRect(screen.Left, screen.Top, screen.Right, screen.Bottom);
    }
}
