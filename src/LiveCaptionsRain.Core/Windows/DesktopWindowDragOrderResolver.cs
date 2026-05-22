namespace LiveCaptionsRain.Core.Windows;

public static class DesktopWindowDragOrderResolver
{
    public static IReadOnlyList<DesktopWindowSnapshot> ApplyDraggedForeground(
        IReadOnlyList<DesktopWindowSnapshot> windowsFromFrontToBack,
        DesktopWindowSnapshot draggedWindow)
    {
        return windowsFromFrontToBack
            .Where(window => window.Handle != draggedWindow.Handle)
            .Prepend(draggedWindow)
            .ToArray();
    }
}
