using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using LiveCaptionsRain.Native;

namespace LiveCaptionsRain.Services;

public sealed class LiveCaptionsService
{
    private const string ProcessName = "LiveCaptions";
    private AutomationElement? _window;
    private AutomationElement? _captionsTextBlock;
    private bool _hiddenByApp;

    public bool IsHiddenByApp => _hiddenByApp;

    public async Task EnsureLaunchedAsync(CancellationToken cancellationToken = default)
    {
        if (TryFindExistingWindow(out _window))
        {
            return;
        }

        try
        {
            _ = Process.Start(ProcessName);
        }
        catch
        {
            return;
        }

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            if (TryFindExistingWindow(out _window))
            {
                return;
            }

            await Task.Delay(150, cancellationToken);
        }
    }

    public string GetCaptions()
    {
        if (_window is null && !TryFindExistingWindow(out _window))
        {
            return string.Empty;
        }

        var window = _window;
        if (window is null)
        {
            return string.Empty;
        }

        try
        {
            _captionsTextBlock ??= FindElementByAutomationId(window, "CaptionsTextBlock");
            return _captionsTextBlock?.Current.Name ?? string.Empty;
        }
        catch (ElementNotAvailableException)
        {
            _window = null;
            _captionsTextBlock = null;
            return string.Empty;
        }
    }

    public void Hide()
    {
        var hWnd = GetNativeWindowHandle();
        if (hWnd == nint.Zero)
        {
            return;
        }

        var exStyle = WindowsApi.GetWindowLong(hWnd, WindowsApi.GwlExStyle);
        WindowsApi.ShowWindow(hWnd, WindowsApi.SwMinimize);
        WindowsApi.SetWindowLong(hWnd, WindowsApi.GwlExStyle, exStyle | WindowsApi.WsExToolWindow);
        _hiddenByApp = true;
    }

    public void Restore()
    {
        var hWnd = GetNativeWindowHandle();
        if (hWnd == nint.Zero)
        {
            return;
        }

        var exStyle = WindowsApi.GetWindowLong(hWnd, WindowsApi.GwlExStyle);
        WindowsApi.SetWindowLong(hWnd, WindowsApi.GwlExStyle, exStyle & ~WindowsApi.WsExToolWindow);
        WindowsApi.ShowWindow(hWnd, WindowsApi.SwRestore);
        WindowsApi.SetForegroundWindow(hWnd);
        _hiddenByApp = false;
    }

    private nint GetNativeWindowHandle()
    {
        if (_window is null && !TryFindExistingWindow(out _window))
        {
            return nint.Zero;
        }

        return new nint(_window!.Current.NativeWindowHandle);
    }

    private static bool TryFindExistingWindow(out AutomationElement? window)
    {
        window = Process.GetProcessesByName(ProcessName)
            .Select(process => FindWindowByProcessId(process.Id))
            .FirstOrDefault(element => element?.Current.ClassName == "LiveCaptionsDesktopWindow");

        return window is not null;
    }

    private static AutomationElement? FindWindowByProcessId(int processId)
    {
        var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);
        return AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
    }

    private static AutomationElement? FindElementByAutomationId(AutomationElement window, string automationId)
    {
        var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
        return window.FindFirst(TreeScope.Descendants, condition);
    }
}
