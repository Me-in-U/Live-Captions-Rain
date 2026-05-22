using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCaptionsRain.Controls;
using LiveCaptionsRain.Core.Localization;
using LiveCaptionsRain.Core.Physics;
using LiveCaptionsRain.Core.Settings;
using LiveCaptionsRain.Core.Text;
using LiveCaptionsRain.Core.Windows;
using LiveCaptionsRain.Services;
using Forms = System.Windows.Forms;
using MediaBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfBorder = System.Windows.Controls.Border;
using WpfSizeChangedEventArgs = System.Windows.SizeChangedEventArgs;

namespace LiveCaptionsRain;

public partial class MainWindow : Window
{
    private const double DemoWordVisualPadding = 6d;
    private const double DemoFloorInset = 18d;
    private const double DemoEdgeSafetyPadding = 24d;

    private readonly MonitorService _monitorService = new();
    private readonly LiveCaptionsService _liveCaptionsService = new();
    private readonly SettingsStore _settingsStore;
    private readonly LocalizedText _text = LocalizedText.For(AppLanguageResolver.Resolve(CultureInfo.CurrentUICulture));
    private LiveCaptionsRainSettings _settings = LiveCaptionsRainSettings.CreateDefault();
    private OverlayWindow? _overlayWindow;
    private Forms.NotifyIcon? _notifyIcon;
    private readonly Random _demoRandom = new(19);
    private readonly NaturalWindField _demoWindField = new(randomSeed: 31);
    private readonly Dictionary<Guid, OutlinedTextBlock> _demoWordElements = [];
    private readonly HashSet<Guid> _handledDemoHighFallImpacts = [];
    private WordPhysicsWorld? _demoWorld;
    private SpawnPlanner _demoSpawnPlanner = new(randomSeed: 19);
    private DateTimeOffset? _lastDemoTick;
    private double _demoSpawnAccumulator;
    private int _demoWordIndex;
    private bool _isDemoRenderingAttached;
    private bool _isApplyingControls;
    private bool _reallyExit;
    private int _currentWordCount;

    public MainWindow()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LiveCaptionsRain",
            "settings.json");
        _settingsStore = new SettingsStore(settingsPath);
        InitializeComponent();
        Closed += (_, _) =>
        {
            StopDemoRendering();
            _demoWorld?.Dispose();
        };
        ApplyLanguage();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = (await _settingsStore.LoadAsync()) with { IsRunning = false };
        PopulateControls();
        ApplyControls(_settings);
        ShowOverlay();
        CreateTrayIcon();
        ResetDemoScene();
        RefreshDemoTimerState();
        StatusText.Text = $"{_text.SettingsStatusPrefix}: {_settingsStore.Path}";
    }

    private void PopulateControls()
    {
        MonitorCombo.ItemsSource = GetLocalizedMonitors();
        SpawnModeCombo.ItemsSource = GetSpawnModeOptions();
        FontWeightCombo.ItemsSource = GetFontWeightOptions();
        FontFamilyCombo.ItemsSource = FontFamilyResolver.GetSystemFonts(CultureInfo.CurrentUICulture);
    }

    private void ApplyControls(LiveCaptionsRainSettings settings)
    {
        _isApplyingControls = true;
        try
        {
            MonitorCombo.SelectedValue = settings.MonitorDeviceName ?? _monitorService.Resolve(null).DeviceName;
            UseAllMonitorsCheck.IsChecked = settings.UseAllMonitors;
            ClickThroughCheck.IsChecked = settings.ClickThrough;
            InteractionModeCheck.IsChecked = settings.InteractionMode;
            StackOnWindowsCheck.IsChecked = settings.StackOnWindows;
            WindowSideWallsCheck.IsChecked = settings.WindowSideWalls;
            FractureOnWordPilesCheck.IsChecked = settings.FractureOnWordPiles;
            RandomWindCheck.IsChecked = settings.RandomWind;
            SpawnModeCombo.SelectedItem = FindOption(GetSpawnModeOptions(), settings.SpawnMode);
            WindStrengthBox.Text = settings.RandomWindStrength.ToString("0.##");
            CaptionDelayBox.Text = settings.CaptionDelayMilliseconds.ToString();
            CleanupLifetimeBox.Text = settings.CleanupLifetimeSeconds.ToString();
            MaxWordsBox.Text = settings.MaxActiveWords.ToString();

            FontFamilyCombo.SelectedValue = settings.FontFamily;
            if (FontFamilyCombo.SelectedValue is null)
            {
                FontFamilyCombo.SelectedValue = "Malgun Gothic";
            }

            FontSizeBox.Text = settings.FontSize.ToString("0.##");
            FontWeightCombo.SelectedItem = FindOption(GetFontWeightOptions(), settings.FontWeight);
            UseFillCheck.IsChecked = settings.UseFill;
            ShadowCheck.IsChecked = settings.Shadow;
            SetColorButton(FontColorButton, FontColorSwatch, settings.FontColor);
            SetColorButton(OutlineColorButton, OutlineColorSwatch, settings.OutlineColor);
            StrokeThicknessBox.Text = settings.StrokeThickness.ToString("0.##");
            OpacitySlider.Value = settings.Opacity;
            UpdateDemoStyle(settings);
            UpdateToggleButtons();
        }
        finally
        {
            _isApplyingControls = false;
        }
    }

    private LiveCaptionsRainSettings ReadControls()
    {
        var selectedMonitor = MonitorCombo.SelectedItem as MonitorInfo;
        return (_settings with
        {
            MonitorDeviceName = selectedMonitor?.DeviceName,
            UseAllMonitors = UseAllMonitorsCheck.IsChecked == true,
            IsRunning = _settings.IsRunning,
            ClickThrough = ClickThroughCheck.IsChecked == true,
            InteractionMode = InteractionModeCheck.IsChecked == true,
            WindowCollision = true,
            StackOnWindows = StackOnWindowsCheck.IsChecked == true,
            WindowSideWalls = WindowSideWallsCheck.IsChecked == true,
            FractureOnWordPiles = FractureOnWordPilesCheck.IsChecked == true,
            RandomWind = RandomWindCheck.IsChecked == true,
            SpawnMode = SpawnModeCombo.SelectedItem is LocalizedOption<SpawnMode> spawnMode ? spawnMode.Value : SpawnMode.Random,
            RandomWindStrength = ParseDouble(WindStrengthBox.Text, _settings.RandomWindStrength),
            CaptionDelayMilliseconds = ParseInt(CaptionDelayBox.Text, _settings.CaptionDelayMilliseconds),
            CleanupLifetimeSeconds = ParseInt(CleanupLifetimeBox.Text, _settings.CleanupLifetimeSeconds),
            MaxActiveWords = ParseInt(MaxWordsBox.Text, _settings.MaxActiveWords),
            FontFamily = FontFamilyCombo.SelectedValue as string ?? _settings.FontFamily,
            FontSize = ParseDouble(FontSizeBox.Text, _settings.FontSize),
            FontWeight = FontWeightCombo.SelectedItem is LocalizedOption<string> fontWeight ? fontWeight.Value : "SemiBold",
            UseFill = UseFillCheck.IsChecked == true,
            Shadow = ShadowCheck.IsChecked == true,
            FontColor = FontColorButton.Tag as string ?? _settings.FontColor,
            OutlineColor = OutlineColorButton.Tag as string ?? _settings.OutlineColor,
            StrokeThickness = ParseDouble(StrokeThicknessBox.Text, _settings.StrokeThickness),
            Opacity = OpacitySlider.Value
        }).Sanitize();
    }

    private async void ApplyAndSave_Click(object sender, RoutedEventArgs e)
    {
        await ApplyAndSaveAsync();
    }

    private async void ToggleRunning_Click(object sender, RoutedEventArgs e)
    {
        await SetRunningAsync(!_settings.IsRunning, save: true);
    }

    private async void LiveCaptionsToggle_Click(object sender, RoutedEventArgs e)
    {
        await ToggleLiveCaptionsAsync();
    }

    private async Task ApplyAndSaveAsync()
    {
        _settings = ReadControls();
        ResetDemoScene();
        ShowOverlay();
        _overlayWindow?.ApplySettings(_settings, _monitorService.Resolve(_settings.MonitorDeviceName));
        _overlayWindow?.SetRunning(_settings.IsRunning);
        SyncTrayChecks();
        await _settingsStore.SaveAsync(_settings);
        StatusText.Text = $"{_text.SavedStatusPrefix}: {_settingsStore.Path}";
    }

    private async Task SetRunningAsync(bool running, bool save)
    {
        _settings = (_settings with { IsRunning = running }).Sanitize();
        _overlayWindow?.SetRunning(_settings.IsRunning);
        UpdateToggleButtons();
        RebuildTrayMenu();

        if (save)
        {
            await _settingsStore.SaveAsync(_settings);
        }
    }

    private async Task ToggleLiveCaptionsAsync()
    {
        await _liveCaptionsService.EnsureLaunchedAsync();
        if (_liveCaptionsService.IsHiddenByApp)
        {
            _liveCaptionsService.Restore();
        }
        else
        {
            _liveCaptionsService.Hide();
        }

        UpdateToggleButtons();
        RebuildTrayMenu();
    }

    private void UpdateToggleButtons()
    {
        ToggleRunningButton.Content = _settings.IsRunning ? _text.TurnOff : _text.TurnOn;
        LiveCaptionsToggleButton.Content = _liveCaptionsService.IsHiddenByApp ? _text.ShowLiveCaptions : _text.HideLiveCaptions;
        RefreshDemoButton.ToolTip = _text.RefreshDemo;
    }

    private void ShowOverlay()
    {
        if (_overlayWindow is not null)
        {
            return;
        }

        _overlayWindow = new OverlayWindow(_settings, _monitorService.Resolve(_settings.MonitorDeviceName), _monitorService, _liveCaptionsService);
        _overlayWindow.ActiveWordCountChanged += UpdateCurrentWordCount;
        _overlayWindow.Show();
        UpdateCurrentWordCount(0);
    }

    private void UpdateCurrentWordCount(int count)
    {
        _currentWordCount = Math.Max(0, count);
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateCurrentWordCount(count));
            return;
        }

        CurrentWordCountText.Text = _text.FormatCurrentWordCount(_currentWordCount);
    }

    private void UpdateDemoStyle(LiveCaptionsRainSettings settings)
    {
        foreach (var element in _demoWordElements.Values)
        {
            ApplyDemoWordStyle(element, settings);
        }
    }

    private void DemoCanvas_SizeChanged(object sender, WpfSizeChangedEventArgs e)
    {
        ResetDemoScene();
    }

    private void RefreshDemo_Click(object sender, RoutedEventArgs e)
    {
        ResetDemoScene();
    }

    private void DemoSpawnSettings_Changed(object sender, RoutedEventArgs e)
    {
        ApplyDemoControlChanges(resetScene: true);
    }

    private void DemoWindSettings_Changed(object sender, RoutedEventArgs e)
    {
        ApplyDemoControlChanges(resetScene: false);
    }

    private void DemoPlatformSettings_Changed(object sender, RoutedEventArgs e)
    {
        ApplyDemoControlChanges(resetScene: true);
    }

    private void ApplyDemoControlChanges(bool resetScene)
    {
        if (_isApplyingControls || !IsLoaded)
        {
            return;
        }

        _settings = ReadControls();
        if (resetScene)
        {
            ResetDemoScene();
            return;
        }

        UpdateDemoStyle(_settings);
    }

    private void DemoRendering_Frame(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        var delta = _lastDemoTick is null ? 1d / 30d : Math.Clamp((now - _lastDemoTick.Value).TotalSeconds, 0, 0.1d);
        _lastDemoTick = now;

        if (DemoCanvas.ActualWidth <= 1 || DemoCanvas.ActualHeight <= 1 || _demoWorld is null)
        {
            return;
        }

        _demoSpawnAccumulator += delta;
        while (_demoSpawnAccumulator >= 0.16d)
        {
            _demoSpawnAccumulator -= 0.16d;
            SpawnDemoWord();
        }

        _demoWorld.Step(delta, word => _settings.RandomWind
            ? _demoWindField.Sample(word.Bounds.CenterX, DemoCanvas.ActualWidth, _settings.RandomWindStrength * 0.18d, now.TimeOfDay.TotalSeconds)
            : WindVector.None);
        FractureDemoHighFallImpacts();
        _demoWorld.ApplyCleanup(maxActiveWords: 80, lifetime: TimeSpan.FromSeconds(3), now);
        RenderDemoWords(_demoWorld.Snapshot());
    }

    private void ResetDemoScene()
    {
        if (!IsLoaded || DemoCanvas.ActualWidth <= 1 || DemoCanvas.ActualHeight <= 1)
        {
            return;
        }

        DemoCanvas.Children.Clear();
        _demoWordElements.Clear();
        _handledDemoHighFallImpacts.Clear();
        _demoWorld?.Dispose();
        _demoWorld = new WordPhysicsWorld(GetDemoPhysicsBounds());
        ApplyDemoWindowPlatforms();
        _demoSpawnPlanner = new SpawnPlanner(randomSeed: _demoRandom.Next());
        _demoWindField.Reset();
        _lastDemoTick = DateTimeOffset.UtcNow;
        _demoSpawnAccumulator = 0;
        _demoWordIndex = 0;

        DrawDemoWindow();

        for (var index = 0; index < 12; index++)
        {
            SpawnDemoWord(initialOffset: TimeSpan.FromMilliseconds(index * 90));
        }
    }

    private void SpawnDemoWord(TimeSpan? initialOffset = null)
    {
        if (_demoWorld is null)
        {
            return;
        }

        var words = DemoSentenceWords();
        var text = words[_demoWordIndex++ % words.Length];
        var size = MeasureDemoWord(text, _settings);
        var area = new ScreenRect(0, 0, DemoCanvas.ActualWidth, DemoCanvas.ActualHeight);
        var x = _demoSpawnPlanner.NextX(_settings.SpawnMode, area, size.Width);
        var y = -size.Height - _demoRandom.NextDouble() * 90;
        var rect = new PhysicsRect(x - size.Width / 2d, y, size.Width, size.Height);
        var createdAt = DateTimeOffset.UtcNow - (initialOffset ?? TimeSpan.Zero);
        var id = _demoWorld.AddWord(
            text,
            rect,
            createdAt,
            initialVelocityX: _demoRandom.NextDouble() * 44 - 22,
            initialVelocityY: 0,
            highFallFractureEnabled: true);

        var element = new OutlinedTextBlock { Text = text, IsHitTestVisible = false };
        ApplyDemoWordStyle(element, _settings);
        _demoWordElements[id] = element;
        DemoCanvas.Children.Add(element);
    }

    private void FractureDemoHighFallImpacts()
    {
        if (_demoWorld is null)
        {
            return;
        }

        IReadOnlyList<PhysicsRect> platforms = _settings.StackOnWindows
            ? [GetDemoWindowTopPlatform()]
            : [];
        var snapshots = _demoWorld.Snapshot();
        foreach (var word in snapshots)
        {
            if (word.IsDeleting || _handledDemoHighFallImpacts.Contains(word.Id))
            {
                continue;
            }

            IEnumerable<PhysicsRect> wordTopPlatforms = _settings.FractureOnWordPiles
                ? snapshots
                    .Where(candidate => candidate.Id != word.Id && !candidate.IsDeleting)
                    .Select(candidate => candidate.Bounds)
                : [];

            if (!HighFallImpactDetector.ShouldFracture(word, _demoWorld.Bounds.Height, platforms, wordTopPlatforms))
            {
                continue;
            }

            _handledDemoHighFallImpacts.Add(word.Id);
            FractureDemoWordIntoFragments(word, WordFractureService.FractureRandomSegments(word.Text, _demoRandom));
        }
    }

    private void FractureDemoWordIntoFragments(PhysicsWordSnapshot word, IReadOnlyList<string> fragments)
    {
        if (_demoWorld is null || fragments.Count <= 1)
        {
            return;
        }

        _demoWorld.Remove(word.Id);
        _handledDemoHighFallImpacts.Remove(word.Id);
        if (_demoWordElements.Remove(word.Id, out var element))
        {
            DemoCanvas.Children.Remove(element);
        }

        var measured = fragments.Select(fragment => (Text: fragment, Size: MeasureDemoWord(fragment, _settings))).ToArray();
        var totalWidth = measured.Sum(item => item.Size.Width);
        var x = word.Bounds.CenterX - totalWidth / 2d;
        foreach (var fragment in measured)
        {
            var rect = new PhysicsRect(x, word.Bounds.Top, fragment.Size.Width, fragment.Size.Height);
            var id = _demoWorld.AddWord(
                fragment.Text,
                rect,
                DateTimeOffset.UtcNow,
                initialVelocityX: _demoRandom.NextDouble() * 180 - 90,
                initialVelocityY: -160 - _demoRandom.NextDouble() * 80,
                highFallFractureEnabled: false);
            var fragmentElement = new OutlinedTextBlock { Text = fragment.Text, IsHitTestVisible = false };
            ApplyDemoWordStyle(fragmentElement, _settings);
            _demoWordElements[id] = fragmentElement;
            DemoCanvas.Children.Add(fragmentElement);
            x += fragment.Size.Width;
        }
    }

    private void RenderDemoWords(IReadOnlyList<PhysicsWordSnapshot> snapshots)
    {
        var ids = snapshots.Select(word => word.Id).ToHashSet();
        foreach (var staleId in _demoWordElements.Keys.Where(id => !ids.Contains(id)).ToArray())
        {
            DemoCanvas.Children.Remove(_demoWordElements[staleId]);
            _demoWordElements.Remove(staleId);
            _handledDemoHighFallImpacts.Remove(staleId);
        }

        foreach (var word in snapshots)
        {
            if (!_demoWordElements.TryGetValue(word.Id, out var element))
            {
                element = new OutlinedTextBlock { Text = word.Text, IsHitTestVisible = false };
                ApplyDemoWordStyle(element, _settings);
                _demoWordElements[word.Id] = element;
                DemoCanvas.Children.Add(element);
            }

            var width = word.Bounds.Width + DemoWordVisualPadding * 2d;
            var height = word.Bounds.Height + DemoWordVisualPadding * 2d;
            if (!string.Equals(element.Text, word.Text, StringComparison.Ordinal))
            {
                element.Text = word.Text;
            }

            if (!DoubleEquals(element.Width, width))
            {
                element.Width = width;
            }

            if (!DoubleEquals(element.Height, height))
            {
                element.Height = height;
            }

            if (element.RenderTransform is not RotateTransform rotate)
            {
                rotate = new RotateTransform();
                element.RenderTransform = rotate;
            }

            var angle = word.AngleRadians;
            rotate.Angle = angle * 180d / Math.PI;
            rotate.CenterX = width / 2d;
            rotate.CenterY = height / 2d;
            var halfVisualWidth = (Math.Abs(Math.Cos(angle)) * width + Math.Abs(Math.Sin(angle)) * height) / 2d + DemoEdgeSafetyPadding;
            var halfVisualHeight = (Math.Abs(Math.Sin(angle)) * width + Math.Abs(Math.Cos(angle)) * height) / 2d + DemoEdgeSafetyPadding;
            var renderCenterX = ClampCenter(word.Bounds.CenterX, halfVisualWidth, DemoCanvas.ActualWidth);
            var renderCenterY = ClampCenter(word.Bounds.CenterY, halfVisualHeight, DemoCanvas.ActualHeight);
            Canvas.SetLeft(element, renderCenterX - width / 2d);
            Canvas.SetTop(element, renderCenterY - height / 2d);
            Panel.SetZIndex(element, 10);
        }
    }

    private ScreenRect GetDemoPhysicsBounds()
    {
        return new ScreenRect(
            0,
            0,
            DemoCanvas.ActualWidth,
            Math.Max(1, DemoCanvas.ActualHeight - DemoFloorInset));
    }

    private void DrawDemoWindow()
    {
        var body = GetDemoWindowRect();
        var frame = new Border
        {
            Width = body.Width,
            Height = body.Height,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 24, 31)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Opacity = 0.96
        };
        Canvas.SetLeft(frame, body.Left);
        Canvas.SetTop(frame, body.Top);
        Panel.SetZIndex(frame, 1);
        DemoCanvas.Children.Add(frame);

        var title = new Border
        {
            Width = body.Width,
            Height = 20,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 44, 54)),
            CornerRadius = new CornerRadius(5, 5, 0, 0)
        };
        Canvas.SetLeft(title, body.Left);
        Canvas.SetTop(title, body.Top);
        Panel.SetZIndex(title, 2);
        DemoCanvas.Children.Add(title);
    }

    private void ApplyDemoWindowPlatforms()
    {
        if (_demoWorld is null)
        {
            return;
        }

        _demoWorld.SetWindowPlatforms(_settings.StackOnWindows
            ? GetDemoWindowCollisionPlatforms()
            : []);
    }

    private PhysicsRect GetDemoWindowRect()
    {
        var width = Math.Clamp(DemoCanvas.ActualWidth * 0.42d, 260, 420);
        var height = Math.Clamp(DemoCanvas.ActualHeight * 0.34d, 70, 96);
        var left = DemoCanvas.ActualWidth * 0.15d;
        var top = DemoCanvas.ActualHeight * 0.5d;
        return new PhysicsRect(left, top, width, height);
    }

    private PhysicsRect GetDemoWindowTopPlatform()
    {
        var rect = GetDemoWindowRect();
        return new PhysicsRect(rect.Left, rect.Top - 2, rect.Width, 4);
    }

    private IReadOnlyList<PhysicsRect> GetDemoWindowCollisionPlatforms()
    {
        var top = GetDemoWindowTopPlatform();
        if (!_settings.WindowSideWalls)
        {
            return [new PhysicsRect(top.Left, top.Top, top.Width, 36)];
        }

        var rect = GetDemoWindowRect();
        const double topThickness = 36d;
        const double sideThickness = 384d;
        var sideTop = rect.Top + topThickness;
        var sideHeight = Math.Max(1, rect.Height - topThickness);
        return
        [
            new PhysicsRect(top.Left, top.Top, top.Width, topThickness),
            new PhysicsRect(rect.Left - sideThickness / 2d, sideTop, sideThickness, sideHeight),
            new PhysicsRect(rect.Right - sideThickness / 2d, sideTop, sideThickness, sideHeight)
        ];
    }

    private void ApplyDemoWordStyle(OutlinedTextBlock element, LiveCaptionsRainSettings settings)
    {
        element.FontFamilyName = settings.FontFamily;
        element.FontSizeValue = settings.FontSize;
        element.FontWeightValue = ToFontWeight(settings.FontWeight);
        element.Fill = BrushFromHex(settings.FontColor);
        element.Stroke = BrushFromHex(settings.OutlineColor);
        element.StrokeThickness = settings.StrokeThickness;
        element.UseFill = settings.UseFill;
        element.Shadow = settings.Shadow;
        element.Opacity = settings.Opacity;
        element.InvalidateVisual();
    }

    private static System.Windows.Size MeasureDemoWord(string text, LiveCaptionsRainSettings settings)
    {
        var typeface = new Typeface(FontFamilyResolver.Create(settings.FontFamily), FontStyles.Normal, ToFontWeight(settings.FontWeight), FontStretches.Normal);
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            settings.FontSize,
            System.Windows.Media.Brushes.White,
            1);
        var padding = Math.Max(10, settings.StrokeThickness * 4 + 8);
        return new System.Windows.Size(
            Math.Max(16, formatted.WidthIncludingTrailingWhitespace + padding),
            Math.Max(16, formatted.Height + padding));
    }

    private static string[] DemoSentenceWords()
    {
        return
        [
            "Live", "Captions", "recognition", "stabilizes", "words", "before", "they", "fall",
            "화면", "위에서", "단어가", "자연스럽게", "떨어지고", "설정한", "글꼴과", "색상으로",
            "겹치며", "쌓이는", "미리보기", "동작입니다"
        ];
    }

    private void CreateTrayIcon()
    {
        if (_notifyIcon is not null)
        {
            return;
        }

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = _text.AppTitle,
            Visible = true,
            Icon = LoadTrayIcon()
        };
        _notifyIcon.DoubleClick += (_, _) => RestoreMainWindow();
        RebuildTrayMenu();
    }

    private void RebuildTrayMenu()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(CheckItem(_text.OnOff, _settings.IsRunning, value => _ = SetRunningAsync(value, save: true)));
        menu.Items.Add(CheckItem(_text.ClickThrough, _settings.ClickThrough, value => UpdateSetting(_settings with { ClickThrough = value })));
        menu.Items.Add(CheckItem(_text.InteractionMode, _settings.InteractionMode, value => UpdateSetting(_settings with { InteractionMode = value })));
        menu.Items.Add(CheckItem(_text.StackOnWindows, _settings.StackOnWindows, value => UpdateSetting(_settings with { StackOnWindows = value })));
        menu.Items.Add(CheckItem(_text.WindowSideWalls, _settings.WindowSideWalls, value => UpdateSetting(_settings with { WindowSideWalls = value })));
        menu.Items.Add(CheckItem(_text.FractureOnWordPiles, _settings.FractureOnWordPiles, value => UpdateSetting(_settings with { FractureOnWordPiles = value })));
        menu.Items.Add(CheckItem(_text.RandomWind, _settings.RandomWind, value => UpdateSetting(_settings with { RandomWind = value })));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_text.ClearWords, null, (_, _) => _overlayWindow?.ClearWords());
        menu.Items.Add(_liveCaptionsService.IsHiddenByApp ? _text.ShowLiveCaptions : _text.HideLiveCaptions, null, async (_, _) => await ToggleLiveCaptionsAsync());

        var monitorMenu = new Forms.ToolStripMenuItem(_text.SelectMonitor);
        foreach (var monitor in GetLocalizedMonitors())
        {
            var item = new Forms.ToolStripMenuItem(monitor.DisplayName)
            {
                Checked = monitor.DeviceName == _settings.MonitorDeviceName
            };
            item.Click += (_, _) => UpdateSetting(_settings with { MonitorDeviceName = monitor.DeviceName, UseAllMonitors = false });
            monitorMenu.DropDownItems.Add(item);
        }

        menu.Items.Add(monitorMenu);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_text.Settings, null, (_, _) => RestoreMainWindow());
        menu.Items.Add(_text.Exit, null, (_, _) => ExitApplication());
        _notifyIcon.ContextMenuStrip = menu;
    }

    private Forms.ToolStripMenuItem CheckItem(string text, bool isChecked, Action<bool> onChanged)
    {
        var item = new Forms.ToolStripMenuItem(text)
        {
            Checked = isChecked,
            CheckOnClick = true
        };
        item.CheckedChanged += (_, _) => onChanged(item.Checked);
        return item;
    }

    private async void UpdateSetting(LiveCaptionsRainSettings settings)
    {
        _settings = settings.Sanitize();
        ApplyControls(_settings);
        _overlayWindow?.ApplySettings(_settings, _monitorService.Resolve(_settings.MonitorDeviceName));
        _overlayWindow?.SetRunning(_settings.IsRunning);
        ResetDemoScene();
        await _settingsStore.SaveAsync(_settings);
        RebuildTrayMenu();
    }

    private void SyncTrayChecks()
    {
        RebuildTrayMenu();
    }

    private void FontColor_Click(object sender, RoutedEventArgs e)
    {
        ChooseColor(FontColorButton, FontColorSwatch);
    }

    private void OutlineColor_Click(object sender, RoutedEventArgs e)
    {
        ChooseColor(OutlineColorButton, OutlineColorSwatch);
    }

    private void ChooseColor(WpfButton button, WpfBorder swatch)
    {
        var current = button.Tag as string ?? "#FFFFFFFF";
        using var dialog = new Forms.ColorDialog
        {
            AllowFullOpen = true,
            FullOpen = true,
            Color = ToDrawingColor(current)
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        SetColorButton(button, swatch, ToHex(dialog.Color));
        _settings = ReadControls();
        UpdateDemoStyle(_settings);
    }

    private static void SetColorButton(WpfButton button, WpfBorder swatch, string hex)
    {
        var normalized = NormalizeColorHex(hex);
        button.Tag = normalized;
        swatch.Background = BrushFromHex(normalized);
    }

    private void RestoreMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        RefreshDemoTimerState();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        RefreshDemoTimerState();
    }

    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        RefreshDemoTimerState();
    }

    private void RefreshDemoTimerState()
    {
        var shouldRun = IsLoaded && IsVisible && WindowState != WindowState.Minimized;
        if (shouldRun)
        {
            _lastDemoTick = DateTimeOffset.UtcNow;
            if (!_isDemoRenderingAttached)
            {
                CompositionTarget.Rendering += DemoRendering_Frame;
                _isDemoRenderingAttached = true;
            }
        }
        else
        {
            StopDemoRendering();
        }
    }

    private void StopDemoRendering()
    {
        if (!_isDemoRenderingAttached)
        {
            return;
        }

        CompositionTarget.Rendering -= DemoRendering_Frame;
        _isDemoRenderingAttached = false;
        _lastDemoTick = null;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_reallyExit)
        {
            _notifyIcon?.Dispose();
            if (_liveCaptionsService.IsHiddenByApp)
            {
                _liveCaptionsService.Restore();
            }

            return;
        }

        e.Cancel = true;
        Hide();
        RefreshDemoTimerState();
        StatusText.Text = _text.MinimizedToTray;
    }

    private async void ShowLiveCaptions_Click(object sender, RoutedEventArgs e)
    {
        await _liveCaptionsService.EnsureLaunchedAsync();
        _liveCaptionsService.Restore();
    }

    private async void HideLiveCaptions_Click(object sender, RoutedEventArgs e)
    {
        await _liveCaptionsService.EnsureLaunchedAsync();
        _liveCaptionsService.Hide();
    }

    private void ClearWords_Click(object sender, RoutedEventArgs e)
    {
        _overlayWindow?.ClearWords();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        ExitApplication();
    }

    private void ExitApplication()
    {
        _reallyExit = true;
        _overlayWindow?.Close();
        Close();
    }

    private static Icon LoadTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LiveCaptionsRain.ico");
        if (File.Exists(iconPath))
        {
            return new Icon(iconPath);
        }

        var executable = Process.GetCurrentProcess().MainModule?.FileName;
        return !string.IsNullOrEmpty(executable) && File.Exists(executable)
            ? System.Drawing.Icon.ExtractAssociatedIcon(executable) ?? SystemIcons.Application
            : SystemIcons.Application;
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static double ParseDouble(string value, double fallback)
    {
        return double.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static bool DoubleEquals(double left, double right)
    {
        return Math.Abs(left - right) < 0.01d;
    }

    private static double ClampCenter(double value, double halfExtent, double totalExtent)
    {
        var min = halfExtent;
        var max = Math.Max(min, totalExtent - halfExtent);
        return Math.Clamp(value, min, max);
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
            return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value)!);
        }
        catch
        {
            return System.Windows.Media.Brushes.White;
        }
    }

    private static System.Drawing.Color ToDrawingColor(string value)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(NormalizeColorHex(value))!;
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        catch
        {
            return System.Drawing.Color.White;
        }
    }

    private static string ToHex(System.Drawing.Color color)
    {
        return $"#FF{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static string NormalizeColorHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "#FFFFFFFF";
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 7 && trimmed.StartsWith('#'))
        {
            return $"#FF{trimmed[1..]}";
        }

        return trimmed;
    }

    private void ApplyLanguage()
    {
        Title = _text.AppTitle;
        RuntimeHeadingText.Text = _text.Runtime;
        MonitorLabelText.Text = _text.Monitor;
        UseAllMonitorsCheck.Content = _text.AllMonitors;
        ClickThroughCheck.Content = _text.ClickThrough;
        InteractionModeCheck.Content = _text.InteractionMode;
        StackOnWindowsCheck.Content = _text.StackOnWindows;
        WindowSideWallsCheck.Content = _text.WindowSideWalls;
        FractureOnWordPilesCheck.Content = _text.FractureOnWordPiles;
        RandomWindCheck.Content = _text.RandomWind;
        SpawnModeLabelText.Text = _text.SpawnMode;
        WindStrengthLabelText.Text = _text.WindStrength;
        CaptionDelayLabelText.Text = _text.CaptionDelayMilliseconds;
        CleanupLifetimeLabelText.Text = _text.CleanupLifetimeSeconds;
        MaxWordsLabelText.Text = _text.MaxActiveWords;
        CurrentWordCountText.Text = _text.FormatCurrentWordCount(_currentWordCount);
        TextStyleHeadingText.Text = _text.TextStyle;
        FontFamilyLabelText.Text = _text.FontFamily;
        FontSizeLabelText.Text = _text.FontSize;
        FontWeightLabelText.Text = _text.FontWeight;
        UseFillCheck.Content = _text.FillText;
        ShadowCheck.Content = _text.Shadow;
        FontColorLabelText.Text = _text.FontColor;
        OutlineColorLabelText.Text = _text.OutlineColor;
        FontColorButtonText.Text = _text.ChooseColor;
        OutlineColorButtonText.Text = _text.ChooseColor;
        StrokeThicknessLabelText.Text = _text.StrokeThickness;
        OpacityLabelText.Text = _text.Opacity;
        RefreshDemoButton.ToolTip = _text.RefreshDemo;
        ApplyAndSaveButton.Content = _text.ApplyAndSave;
        ExitButton.Content = _text.Exit;
        UpdateToggleButtons();
    }

    private MonitorInfo[] GetLocalizedMonitors()
    {
        return _monitorService.GetMonitors()
            .Select((monitor, index) => monitor with
            {
                DisplayName = _text.FormatMonitor(monitor.IsPrimary, index + 1, monitor.Bounds.Width, monitor.Bounds.Height)
            })
            .ToArray();
    }

    private LocalizedOption<SpawnMode>[] GetSpawnModeOptions()
    {
        return
        [
            new(SpawnMode.Random, _text.SpawnModeRandom),
            new(SpawnMode.LeftToRight, _text.SpawnModeLeftToRight),
            new(SpawnMode.RightToLeft, _text.SpawnModeRightToLeft),
            new(SpawnMode.CenterBiased, _text.SpawnModeCenterBiased)
        ];
    }

    private LocalizedOption<string>[] GetFontWeightOptions()
    {
        return
        [
            new("Regular", _text.FontWeightRegular),
            new("Medium", _text.FontWeightMedium),
            new("SemiBold", _text.FontWeightSemiBold),
            new("Bold", _text.FontWeightBold),
            new("Black", _text.FontWeightBlack)
        ];
    }

    private static LocalizedOption<T>? FindOption<T>(LocalizedOption<T>[] options, T value)
    {
        return options.FirstOrDefault(option => Equals(option.Value, value));
    }

    private sealed record LocalizedOption<T>(T Value, string Display)
    {
        public override string ToString() => Display;
    }

}
