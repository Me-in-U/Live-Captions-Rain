using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using LiveCaptionsRain.Controls;
using LiveCaptionsRain.Core.Captions;
using LiveCaptionsRain.Core.Physics;
using LiveCaptionsRain.Core.Settings;
using LiveCaptionsRain.Core.Text;
using LiveCaptionsRain.Core.Timing;
using LiveCaptionsRain.Core.Windows;
using LiveCaptionsRain.Native;
using LiveCaptionsRain.Services;
using MediaBrush = System.Windows.Media.Brush;
using WpfSize = System.Windows.Size;

namespace LiveCaptionsRain;

public partial class OverlayWindow : Window
{
    private static readonly TimeSpan CaptionSettleMinimum = TimeSpan.FromMilliseconds(900);
    private static readonly TimeSpan CaptionSettleMaximum = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan OverflowFadeDuration = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StableWindowColliderRefreshInterval = TimeSpan.FromMilliseconds(260);
    private static readonly TimeSpan DragWindowColliderRefreshInterval = TimeSpan.FromMilliseconds(130);
    private const double StableWindowPlatformTolerancePixels = 2d;
    private const double DragWindowPlatformTolerancePixels = 0.25d;
    private const double WordVisualOffsetYPixels = 8d;

    private readonly LiveCaptionsService _liveCaptions;
    private readonly MonitorService _monitorService;
    private readonly DesktopWindowService _desktopWindowService = new();
    private readonly CaptionWordPipeline _captionPipeline = new();
    private readonly SpawnPlanner _spawnPlanner = new();
    private readonly NaturalWindField _windField = new();
    private readonly DispatcherTimer _captionTimer;
    private readonly DispatcherTimer _spawnTimer;
    private readonly Random _random = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Dictionary<Guid, OutlinedTextBlock> _wordElements = [];
    private readonly HashSet<Guid> _handledHighFallImpacts = [];

    private LiveCaptionsRainSettings _settings;
    private MonitorInfo _monitor;
    private WordPhysicsWorld? _world;
    private ScreenRect _absoluteBounds;
    private IReadOnlyList<DesktopWindowSnapshot> _windowSnapshots = [];
    private IReadOnlyList<WindowColliderSnapshot> _colliders = [];
    private IReadOnlyList<PhysicsRect> _windowPlatforms = [];
    private TimeSpan _lastFrame = TimeSpan.Zero;
    private TimeSpan _lastColliderRefresh = TimeSpan.Zero;
    private TimeSpan _captionSettleMinimumUntil = TimeSpan.Zero;
    private TimeSpan _captionSettleDeadline = TimeSpan.Zero;
    private string _lastSettleCaption = string.Empty;
    private int _stableSettleTicks;
    private int _lastReportedWordCount = -1;
    private nint _handle;
    private nint _dragWindowHandle;
    private bool _isInitialized;
    private bool _isRenderingAttached;
    private TimeSpan _lastSpawnPump = TimeSpan.Zero;
    private string _lastRenderedWordStyleKey = string.Empty;

    public event Action<int>? ActiveWordCountChanged;

    public OverlayWindow(LiveCaptionsRainSettings settings, MonitorInfo monitor, MonitorService monitorService, LiveCaptionsService liveCaptions)
    {
        _settings = settings;
        _monitor = monitor;
        _monitorService = monitorService;
        _liveCaptions = liveCaptions;

        InitializeComponent();
        _captionPipeline.SetStabilizationDelay(TimeSpan.FromMilliseconds(_settings.CaptionDelayMilliseconds));

        _captionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
        _captionTimer.Tick += CaptionTimer_Tick;
        _spawnTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _spawnTimer.Tick += SpawnTimer_Tick;

        SourceInitialized += (_, _) =>
        {
            _handle = new WindowInteropHelper(this).Handle;
            ApplyClickThrough();
        };

        Loaded += OverlayWindow_Loaded;
        Closed += OverlayWindow_Closed;
    }

    public void ApplySettings(LiveCaptionsRainSettings settings, MonitorInfo monitor)
    {
        _settings = settings.Sanitize();
        _monitor = monitor;
        _captionPipeline.SetStabilizationDelay(TimeSpan.FromMilliseconds(_settings.CaptionDelayMilliseconds));
        PositionOverTarget();
        _lastColliderRefresh = TimeSpan.MinValue;
        _windowPlatforms = [];
        ApplyClickThrough();
        UpdateOverlayActivity();
    }

    public void ClearWords()
    {
        _world?.Clear();
        foreach (var element in _wordElements.Values)
        {
            WordCanvas.Children.Remove(element);
        }

        _wordElements.Clear();
        _handledHighFallImpacts.Clear();
        _captionPipeline.Clear(_liveCaptions.GetCaptions());
        ReportActiveWordCount(0);
        BeginCaptionSettle();
        StopSpawnPump();
        UpdateOverlayActivity();
    }

    public void SetRunning(bool running)
    {
        _settings = _settings with { IsRunning = running };
        if (!running)
        {
            StopSpawnPump();
        }

        UpdateOverlayActivity();
    }

    private async void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            UpdateOverlayActivity();
            return;
        }

        _isInitialized = true;
        PositionOverTarget();
        UpdateOverlayActivity();
        await _liveCaptions.EnsureLaunchedAsync();
        _captionPipeline.ObserveExisting(_liveCaptions.GetCaptions());
        BeginCaptionSettle();
        _captionTimer.Start();
        ReportActiveWordCount(0);
        UpdateOverlayActivity();
    }

    private void OverlayWindow_Closed(object? sender, EventArgs e)
    {
        _captionTimer.Stop();
        StopSpawnPump();
        StopRendering();
        _world?.Dispose();
    }

    private void PositionOverTarget()
    {
        _absoluteBounds = _settings.UseAllMonitors ? _monitorService.GetVirtualBounds() : _monitor.Bounds;

        Left = _absoluteBounds.Left;
        Top = _absoluteBounds.Top;
        Width = Math.Max(1, _absoluteBounds.Width);
        Height = Math.Max(1, _absoluteBounds.Height);

        var localBounds = new ScreenRect(0, 0, _absoluteBounds.Width, _absoluteBounds.Height);
        if (_world is null)
        {
            _world = new WordPhysicsWorld(localBounds);
        }
        else
        {
            _world.SetBounds(localBounds);
        }

        _windField.Reset();
        _windowPlatforms = [];
        _windowSnapshots = [];
        _dragWindowHandle = nint.Zero;
    }

    private void EnsureOverlayActive()
    {
        if (!IsVisible)
        {
            Show();
            PositionOverTarget();
            ApplyClickThrough();
        }

        StartRendering();
    }

    private void UpdateOverlayActivity()
    {
        var hasRenderableWords = _world?.WordCount > 0 || _wordElements.Count > 0;
        if (hasRenderableWords)
        {
            EnsureOverlayActive();
            return;
        }

        StopRendering();
        if (IsVisible)
        {
            Hide();
        }
    }

    private void StartRendering()
    {
        if (_isRenderingAttached)
        {
            return;
        }

        _lastFrame = TimeSpan.Zero;
        CompositionTarget.Rendering += CompositionTarget_Rendering;
        _isRenderingAttached = true;
    }

    private void StopRendering()
    {
        if (!_isRenderingAttached)
        {
            return;
        }

        CompositionTarget.Rendering -= CompositionTarget_Rendering;
        _isRenderingAttached = false;
        _lastFrame = TimeSpan.Zero;
    }

    private void StartSpawnPumpIfNeeded()
    {
        if (!_settings.IsRunning || _captionPipeline.PendingCount == 0 || _spawnTimer.IsEnabled)
        {
            return;
        }

        _lastSpawnPump = TimeSpan.Zero;
        _spawnTimer.Start();
    }

    private void StopSpawnPump()
    {
        if (!_spawnTimer.IsEnabled)
        {
            return;
        }

        _spawnTimer.Stop();
        _lastSpawnPump = TimeSpan.Zero;
    }

    private void ApplyClickThrough()
    {
        if (_handle == nint.Zero)
        {
            return;
        }

        var style = WindowsApi.GetWindowLong(_handle, WindowsApi.GwlExStyle);
        var shouldPassThrough = _settings.ClickThrough;
        var next = shouldPassThrough
            ? style | WindowsApi.WsExTransparent
            : style & ~WindowsApi.WsExTransparent;
        WindowsApi.SetWindowLong(_handle, WindowsApi.GwlExStyle, next);
        WordCanvas.IsHitTestVisible = !shouldPassThrough;
    }

    private void CaptionTimer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.IsRunning || _world is null)
        {
            return;
        }

        var captions = _liveCaptions.GetCaptions();
        if (IsCaptionSettling(captions))
        {
            _captionPipeline.AbsorbCaptionUpdate(captions);
            StopSpawnPump();
            return;
        }

        _captionPipeline.AddCaptionUpdate(captions, _clock.Elapsed);
        StartSpawnPumpIfNeeded();
    }

    private void SpawnTimer_Tick(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        var delta = _lastSpawnPump == TimeSpan.Zero ? _spawnTimer.Interval.TotalSeconds : (now - _lastSpawnPump).TotalSeconds;
        _lastSpawnPump = now;
        FlushSpawnQueue(delta);
    }

    private void BeginCaptionSettle()
    {
        var now = _clock.Elapsed;
        _captionSettleMinimumUntil = now + CaptionSettleMinimum;
        _captionSettleDeadline = now + CaptionSettleMaximum;
        _lastSettleCaption = string.Empty;
        _stableSettleTicks = 0;
    }

    private bool IsCaptionSettling(string captions)
    {
        var now = _clock.Elapsed;
        if (now >= _captionSettleDeadline)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(captions))
        {
            _stableSettleTicks = string.Equals(captions, _lastSettleCaption, StringComparison.Ordinal)
                ? _stableSettleTicks + 1
                : 1;
            _lastSettleCaption = captions;
        }

        return now < _captionSettleMinimumUntil || _stableSettleTicks < 2;
    }

    private void SpawnWord(string word)
    {
        if (_world is null || string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        var size = MeasureWord(word);
        var x = _spawnPlanner.NextX(_settings.SpawnMode, _world.Bounds, size.Width);
        var rect = new PhysicsRect(x - size.Width / 2d, -size.Height - 8, size.Width, size.Height);
        _world.AddWord(word, rect, DateTimeOffset.UtcNow, initialVelocityX: _random.NextDouble() * 80 - 40, initialVelocityY: 0);
        StartOverflowCleanup();
        EnsureOverlayActive();
    }

    private WpfSize MeasureWord(string text)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(FontFamilyResolver.Create(_settings.FontFamily), FontStyles.Normal, ToFontWeight(_settings.FontWeight), FontStretches.Normal);
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            _settings.FontSize,
            Brushes.White,
            dpi);

        var padding = Math.Max(10, _settings.StrokeThickness * 4 + 8);
        return new WpfSize(
            Math.Max(12, formatted.WidthIncludingTrailingWhitespace + padding),
            Math.Max(12, formatted.Height + padding));
    }

    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        if (_world is null)
        {
            return;
        }

        if (_world.WordCount == 0)
        {
            RenderWords([]);
            UpdateOverlayActivity();
            return;
        }

        var now = _clock.Elapsed;
        var delta = _lastFrame == TimeSpan.Zero ? TimeSpan.FromSeconds(1d / 60d) : now - _lastFrame;
        _lastFrame = now;

        RefreshWindowColliders(now);

        _world.Step(delta.TotalSeconds, word => _settings.RandomWind
            ? _windField.Sample(word.Bounds.CenterX, _world.Bounds.Width, _settings.RandomWindStrength, now.TotalSeconds)
            : WindVector.None);
        FractureHighFallImpacts();
        _world.ApplyCleanup(_settings.MaxActiveWords, TimeSpan.FromSeconds(_settings.CleanupLifetimeSeconds), DateTimeOffset.UtcNow);
        RenderWords(_world.Snapshot());
        UpdateOverlayActivity();
    }

    private void FlushSpawnQueue(double deltaSeconds)
    {
        if (!_settings.IsRunning || _world is null)
        {
            StopSpawnPump();
            return;
        }

        foreach (var word in _captionPipeline.Drain(deltaSeconds))
        {
            SpawnWord(word);
        }

        if (_captionPipeline.PendingCount == 0)
        {
            StopSpawnPump();
        }
    }

    private void RefreshWindowColliders(TimeSpan now)
    {
        var isDraggingWindow = IsLeftMouseButtonPressed();
        if (_world is null)
        {
            return;
        }

        if (isDraggingWindow && TryRefreshDraggedWindowCollider())
        {
            return;
        }

        _dragWindowHandle = nint.Zero;
        var refreshInterval = isDraggingWindow
            ? DragWindowColliderRefreshInterval
            : StableWindowColliderRefreshInterval;
        if (!FrameCadence.ShouldRun(now, _lastColliderRefresh, refreshInterval))
        {
            return;
        }

        _lastColliderRefresh = now;
        if (_settings.StackOnWindows)
        {
            var state = _desktopWindowService.GetWindowCollisionState(_absoluteBounds, _handle);
            _windowSnapshots = state.Snapshots;
            ApplyWindowColliders(state.Colliders, StableWindowPlatformTolerancePixels);
            return;
        }

        _windowSnapshots = [];
        ApplyWindowColliders([], StableWindowPlatformTolerancePixels);
    }

    private bool TryRefreshDraggedWindowCollider()
    {
        if (!_settings.StackOnWindows)
        {
            return false;
        }

        if (_windowSnapshots.Count == 0 || !TryResolveDraggedWindowSnapshot(out var draggedWindow))
        {
            return false;
        }

        _windowSnapshots = DesktopWindowDragOrderResolver.ApplyDraggedForeground(_windowSnapshots, draggedWindow);
        var nextColliders = _desktopWindowService.ResolveWindowColliders(_windowSnapshots, _absoluteBounds);
        ApplyWindowColliders(nextColliders, DragWindowPlatformTolerancePixels);
        return true;
    }

    private bool TryResolveDraggedWindowSnapshot(out DesktopWindowSnapshot snapshot)
    {
        snapshot = default!;
        foreach (var handle in GetDraggedWindowCandidates())
        {
            if (handle == nint.Zero)
            {
                continue;
            }

            var root = WindowsApi.GetAncestor(handle, WindowsApi.GaRoot);
            if (root == nint.Zero)
            {
                root = handle;
            }

            if (_desktopWindowService.TryGetWindowSnapshot(_absoluteBounds, _handle, root, out snapshot))
            {
                _dragWindowHandle = root;
                return true;
            }
        }

        _dragWindowHandle = nint.Zero;
        return false;
    }

    private IEnumerable<nint> GetDraggedWindowCandidates()
    {
        if (_dragWindowHandle != nint.Zero)
        {
            yield return _dragWindowHandle;
        }

        if (WindowsApi.GetCursorPos(out var point))
        {
            yield return WindowsApi.WindowFromPoint(point);
        }

        yield return WindowsApi.GetForegroundWindow();
    }

    private void ApplyWindowColliders(IReadOnlyList<WindowColliderSnapshot> nextColliders, double tolerance)
    {
        var nextPlatforms = nextColliders.SelectMany(item => item.TopPlatforms).ToArray();
        _colliders = nextColliders;
        if (PhysicsRectSetComparer.AreEquivalent(_windowPlatforms, nextPlatforms, tolerance))
        {
            return;
        }

        _windowPlatforms = nextPlatforms;
        _world?.SetWindowPlatforms(nextPlatforms);
    }

    private static bool IsLeftMouseButtonPressed()
    {
        return (WindowsApi.GetAsyncKeyState(WindowsApi.VkLButton) & unchecked((short)0x8000)) != 0;
    }

    private void FractureHighFallImpacts()
    {
        if (_world is null)
        {
            return;
        }

        var platforms = _colliders.SelectMany(item => item.TopPlatforms).ToArray();
        if (!_settings.StackOnWindows)
        {
            platforms = [];
        }

        var snapshots = _world.Snapshot();
        foreach (var word in snapshots)
        {
            if (word.IsDeleting)
            {
                continue;
            }

            if (_handledHighFallImpacts.Contains(word.Id))
            {
                continue;
            }

            IEnumerable<PhysicsRect> wordTopPlatforms = _settings.FractureOnWordPiles
                ? snapshots
                    .Where(candidate => candidate.Id != word.Id && !candidate.IsDeleting)
                    .Select(candidate => candidate.Bounds)
                : [];

            if (!HighFallImpactDetector.ShouldFracture(word, _world.Bounds.Height, platforms, wordTopPlatforms))
            {
                continue;
            }

            _handledHighFallImpacts.Add(word.Id);
            FractureWordIntoFragments(word, WordFractureService.FractureRandomSegments(word.Text, _random));
        }
    }

    private void FractureWordIntoFragments(PhysicsWordSnapshot word, IReadOnlyList<string> fragments)
    {
        if (_world is null)
        {
            return;
        }

        if (fragments.Count <= 1)
        {
            return;
        }

        _world.Remove(word.Id);
        _handledHighFallImpacts.Remove(word.Id);
        if (_wordElements.Remove(word.Id, out var element))
        {
            WordCanvas.Children.Remove(element);
        }

        var measured = fragments.Select(fragment => (Text: fragment, Size: MeasureWord(fragment))).ToArray();
        var totalWidth = measured.Sum(item => item.Size.Width);
        var x = word.Bounds.CenterX - totalWidth / 2d;
        foreach (var fragment in measured)
        {
            var rect = new PhysicsRect(x, word.Bounds.Top, fragment.Size.Width, fragment.Size.Height);
            _world.AddWord(
                fragment.Text,
                rect,
                DateTimeOffset.UtcNow,
                initialVelocityX: _random.NextDouble() * 260 - 130,
                initialVelocityY: -240 - _random.NextDouble() * 140,
                highFallFractureEnabled: false);
            x += fragment.Size.Width;
        }

        StartOverflowCleanup();
    }

    private void StartOverflowCleanup()
    {
        _world?.StartOverflowCleanup(_settings.MaxActiveWords, OverflowFadeDuration, DateTimeOffset.UtcNow);
    }

    private void RenderWords(IReadOnlyList<PhysicsWordSnapshot> snapshots)
    {
        ReportActiveWordCount(snapshots.Count);
        var activeIds = snapshots.Select(item => item.Id).ToHashSet();
        foreach (var stale in _wordElements.Keys.Where(id => !activeIds.Contains(id)).ToArray())
        {
            WordCanvas.Children.Remove(_wordElements[stale]);
            _wordElements.Remove(stale);
            _handledHighFallImpacts.Remove(stale);
        }

        var styleKey = BuildWordStyleKey();
        var styleChanged = !string.Equals(styleKey, _lastRenderedWordStyleKey, StringComparison.Ordinal);
        var fontWeight = ToFontWeight(_settings.FontWeight);
        var fill = styleChanged ? BrushFromHex(_settings.FontColor) : null;
        var stroke = styleChanged ? BrushFromHex(_settings.OutlineColor) : null;
        foreach (var word in snapshots)
        {
            var element = GetOrCreateElement(word.Id, out var created);
            var (displayText, opacity) = GetDisplayTextAndOpacity(word);
            var visualChanged = created || styleChanged;
            if (!string.Equals(element.Text, displayText, StringComparison.Ordinal))
            {
                element.Text = displayText;
                visualChanged = true;
            }

            var width = word.Bounds.Width + 12;
            var height = word.Bounds.Height + 12;
            if (!DoubleEquals(element.Width, width))
            {
                element.Width = width;
            }

            if (!DoubleEquals(element.Height, height))
            {
                element.Height = height;
            }

            if (created || styleChanged)
            {
                element.FontFamilyName = _settings.FontFamily;
                element.FontSizeValue = _settings.FontSize;
                element.FontWeightValue = fontWeight;
                element.Fill = fill ?? BrushFromHex(_settings.FontColor);
                element.Stroke = stroke ?? BrushFromHex(_settings.OutlineColor);
                element.StrokeThickness = _settings.StrokeThickness;
                element.VisualOffsetY = WordVisualOffsetYPixels;
                element.UseFill = _settings.UseFill;
                element.Shadow = _settings.Shadow;
            }

            element.Opacity = opacity * _settings.Opacity;
            if (element.RenderTransform is not RotateTransform rotate)
            {
                rotate = new RotateTransform();
                element.RenderTransform = rotate;
            }

            rotate.Angle = word.AngleRadians * 180d / Math.PI;
            rotate.CenterX = width / 2d;
            rotate.CenterY = height / 2d;
            Canvas.SetLeft(element, word.Bounds.Left - 6);
            Canvas.SetTop(element, word.Bounds.Top - 6);
            if (visualChanged)
            {
                element.InvalidateVisual();
            }
        }

        _lastRenderedWordStyleKey = styleKey;
    }

    private (string Text, double Opacity) GetDisplayTextAndOpacity(PhysicsWordSnapshot word)
    {
        var lifetime = TimeSpan.FromSeconds(_settings.CleanupLifetimeSeconds);
        var age = DateTimeOffset.UtcNow - word.CreatedAt;
        if (word.IsDeleting && word.DeleteAt is not null)
        {
            var startedAt = word.DeletionStartedAt ?? word.DeleteAt.Value - OverflowFadeDuration;
            var duration = word.DeleteAt.Value - startedAt;
            var deleteRemaining = word.DeleteAt.Value - DateTimeOffset.UtcNow;
            var deleteOpacity = duration.TotalSeconds <= 0
                ? 0.02d
                : Math.Clamp(deleteRemaining.TotalSeconds / duration.TotalSeconds, 0.02d, 1d);
            return (word.Text, deleteOpacity);
        }

        var remaining = lifetime - age;
        if (remaining > TimeSpan.FromSeconds(2))
        {
            return (word.Text, 1);
        }

        var opacity = Math.Clamp(remaining.TotalSeconds / 2d, 0.08d, 1d);
        var burnedText = HangulFragmenter.DecomposeForBurn(word.Text);
        return (burnedText == word.Text ? word.Text : burnedText, opacity);
    }

    private OutlinedTextBlock GetOrCreateElement(Guid id, out bool created)
    {
        if (_wordElements.TryGetValue(id, out var element))
        {
            created = false;
            return element;
        }

        element = new OutlinedTextBlock { IsHitTestVisible = false };
        _wordElements[id] = element;
        WordCanvas.Children.Add(element);
        created = true;
        return element;
    }

    private string BuildWordStyleKey()
    {
        return string.Join(
            '\u001f',
            _settings.FontFamily,
            _settings.FontSize.ToString("R", CultureInfo.InvariantCulture),
            _settings.FontWeight,
            _settings.FontColor,
            _settings.OutlineColor,
            _settings.StrokeThickness.ToString("R", CultureInfo.InvariantCulture),
            _settings.UseFill,
            _settings.Shadow);
    }

    private static bool DoubleEquals(double left, double right)
    {
        return Math.Abs(left - right) < 0.01d;
    }

    private void ReportActiveWordCount(int count)
    {
        if (_lastReportedWordCount == count)
        {
            return;
        }

        _lastReportedWordCount = count;
        ActiveWordCountChanged?.Invoke(count);
    }

    private static FontWeight ToFontWeight(string value)
    {
        try
        {
            return (FontWeight)new FontWeightConverter().ConvertFromString(value)!;
        }
        catch
        {
            return FontWeights.SemiBold;
        }
    }

    private static MediaBrush BrushFromHex(string value)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)!);
        }
        catch
        {
            return Brushes.White;
        }
    }
}
