using System.Globalization;

namespace LiveCaptionsRain.Core.Localization;

public sealed record LocalizedText
{
    public required string AppTitle { get; init; }
    public required string Subtitle { get; init; }
    public required string ClearWords { get; init; }
    public required string ShowLiveCaptions { get; init; }
    public required string HideLiveCaptions { get; init; }
    public required string Runtime { get; init; }
    public required string Monitor { get; init; }
    public required string PrimaryMonitor { get; init; }
    public required string MonitorName { get; init; }
    public required string AllMonitors { get; init; }
    public required string Running { get; init; }
    public required string ClickThrough { get; init; }
    public required string InteractionMode { get; init; }
    public required string WindowCollision { get; init; }
    public required string StackOnWindows { get; init; }
    public required string FractureOnWordPiles { get; init; }
    public required string RandomWind { get; init; }
    public required string SpawnMode { get; init; }
    public required string WindStrength { get; init; }
    public required string CaptionDelayMilliseconds { get; init; }
    public required string CleanupLifetimeSeconds { get; init; }
    public required string MaxActiveWords { get; init; }
    public required string CurrentWordCountFormat { get; init; }
    public required string TextStyle { get; init; }
    public required string FontFamily { get; init; }
    public required string FontSize { get; init; }
    public required string FontWeight { get; init; }
    public required string FillText { get; init; }
    public required string Shadow { get; init; }
    public required string FontColor { get; init; }
    public required string OutlineColor { get; init; }
    public required string StrokeThickness { get; init; }
    public required string Opacity { get; init; }
    public required string PreviewText { get; init; }
    public required string Apply { get; init; }
    public required string Save { get; init; }
    public required string ApplyAndSave { get; init; }
    public required string TurnOn { get; init; }
    public required string TurnOff { get; init; }
    public required string OnOff { get; init; }
    public required string RefreshDemo { get; init; }
    public required string ChooseColor { get; init; }
    public required string Exit { get; init; }
    public required string SettingsStatusPrefix { get; init; }
    public required string SavedStatusPrefix { get; init; }
    public required string AppliedStatus { get; init; }
    public required string MinimizedToTray { get; init; }
    public required string SelectMonitor { get; init; }
    public required string Settings { get; init; }
    public required string SpawnModeRandom { get; init; }
    public required string SpawnModeLeftToRight { get; init; }
    public required string SpawnModeRightToLeft { get; init; }
    public required string SpawnModeCenterBiased { get; init; }
    public required string FontWeightRegular { get; init; }
    public required string FontWeightMedium { get; init; }
    public required string FontWeightSemiBold { get; init; }
    public required string FontWeightBold { get; init; }
    public required string FontWeightBlack { get; init; }

    public static LocalizedText For(AppLanguage language)
    {
        return language == AppLanguage.Korean ? Korean : English;
    }

    public string FormatMonitor(bool isPrimary, int index, double width, double height)
    {
        var prefix = isPrimary ? PrimaryMonitor : MonitorName;
        return $"{prefix} {index} ({width:0}x{height:0})";
    }

    public string FormatCurrentWordCount(int count)
    {
        return string.Format(CultureInfo.InvariantCulture, CurrentWordCountFormat, count);
    }

    private static LocalizedText English { get; } = new()
    {
        AppTitle = "Live Captions Rain",
        Subtitle = "Live Captions words falling into the desktop",
        ClearWords = "Clear Words",
        ShowLiveCaptions = "Show Live Captions",
        HideLiveCaptions = "Hide Live Captions",
        Runtime = "Runtime",
        Monitor = "Monitor",
        PrimaryMonitor = "Primary",
        MonitorName = "Monitor",
        AllMonitors = "All monitors",
        Running = "Drop words",
        ClickThrough = "Click-through",
        InteractionMode = "Click interaction",
        WindowCollision = "Use windows as floor",
        StackOnWindows = "Stack on windows",
        FractureOnWordPiles = "Break on word piles",
        RandomWind = "Apply wind",
        SpawnMode = "Spawn mode",
        WindStrength = "Wind strength",
        CaptionDelayMilliseconds = "Caption delay (ms)",
        CleanupLifetimeSeconds = "Cleanup lifetime seconds",
        MaxActiveWords = "Max active words",
        CurrentWordCountFormat = "(current: {0} shown)",
        TextStyle = "Text Style",
        FontFamily = "Font family",
        FontSize = "Font size",
        FontWeight = "Font weight",
        FillText = "Fill text",
        Shadow = "Shadow",
        FontColor = "Font color",
        OutlineColor = "Outline color",
        StrokeThickness = "Stroke thickness",
        Opacity = "Opacity",
        PreviewText = "Live captions fall like this",
        Apply = "Apply",
        Save = "Save",
        ApplyAndSave = "Apply and Save",
        TurnOn = "Turn On",
        TurnOff = "Turn Off",
        OnOff = "On/Off",
        RefreshDemo = "Refresh",
        ChooseColor = "Choose color",
        Exit = "Exit",
        SettingsStatusPrefix = "Settings",
        SavedStatusPrefix = "Saved",
        AppliedStatus = "Applied to overlay",
        MinimizedToTray = "Minimized to tray",
        SelectMonitor = "Select monitor",
        Settings = "Settings",
        SpawnModeRandom = "Random",
        SpawnModeLeftToRight = "Left to right",
        SpawnModeRightToLeft = "Right to left",
        SpawnModeCenterBiased = "Center biased",
        FontWeightRegular = "Regular",
        FontWeightMedium = "Medium",
        FontWeightSemiBold = "SemiBold",
        FontWeightBold = "Bold",
        FontWeightBlack = "Black"
    };

    private static LocalizedText Korean { get; } = new()
    {
        AppTitle = "Live Captions Rain",
        Subtitle = "라이브 캡션 단어가 화면에 떨어집니다",
        ClearWords = "단어 지우기",
        ShowLiveCaptions = "라이브 캡션 표시",
        HideLiveCaptions = "라이브 캡션 숨기기",
        Runtime = "실행",
        Monitor = "모니터",
        PrimaryMonitor = "기본 모니터",
        MonitorName = "모니터",
        AllMonitors = "전체 모니터",
        Running = "단어 떨어뜨리기",
        ClickThrough = "클릭 통과",
        InteractionMode = "클릭 상호작용",
        WindowCollision = "창을 바닥으로 사용",
        StackOnWindows = "창 위에 쌓기",
        FractureOnWordPiles = "단어 더미 충돌 분리",
        RandomWind = "바람 적용",
        SpawnMode = "생성 위치",
        WindStrength = "바람 세기",
        CaptionDelayMilliseconds = "캡션 안정화 지연(ms)",
        CleanupLifetimeSeconds = "정리 시간(초)",
        MaxActiveWords = "최대 단어 수",
        CurrentWordCountFormat = "(현재: {0}개 표시중)",
        TextStyle = "텍스트 스타일",
        FontFamily = "글꼴",
        FontSize = "글꼴 크기",
        FontWeight = "굵기",
        FillText = "텍스트 채움",
        Shadow = "그림자",
        FontColor = "글자 색상",
        OutlineColor = "외곽선 색상",
        StrokeThickness = "외곽선 두께",
        Opacity = "불투명도",
        PreviewText = "라이브 캡션이 이렇게 떨어집니다",
        Apply = "적용",
        Save = "저장",
        ApplyAndSave = "적용 및 저장",
        TurnOn = "켜기",
        TurnOff = "끄기",
        OnOff = "ON/OFF",
        RefreshDemo = "새로고침",
        ChooseColor = "색상 선택",
        Exit = "종료",
        SettingsStatusPrefix = "설정",
        SavedStatusPrefix = "저장됨",
        AppliedStatus = "오버레이에 적용됨",
        MinimizedToTray = "트레이로 최소화됨",
        SelectMonitor = "모니터 선택",
        Settings = "설정",
        SpawnModeRandom = "랜덤",
        SpawnModeLeftToRight = "왼쪽에서 오른쪽",
        SpawnModeRightToLeft = "오른쪽에서 왼쪽",
        SpawnModeCenterBiased = "중앙 위주",
        FontWeightRegular = "보통",
        FontWeightMedium = "중간",
        FontWeightSemiBold = "약간 굵게",
        FontWeightBold = "굵게",
        FontWeightBlack = "매우 굵게"
    };
}
