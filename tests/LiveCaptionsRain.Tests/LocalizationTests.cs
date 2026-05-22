using System.Globalization;
using LiveCaptionsRain.Core.Localization;

namespace LiveCaptionsRain.Tests;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData("ko-KR")]
    [InlineData("ko")]
    public void Resolve_uses_korean_for_korean_display_language(string cultureName)
    {
        var language = AppLanguageResolver.Resolve(new CultureInfo(cultureName));

        Assert.Equal(AppLanguage.Korean, language);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ja-JP")]
    public void Resolve_uses_english_as_default_fallback(string cultureName)
    {
        var language = AppLanguageResolver.Resolve(new CultureInfo(cultureName));

        Assert.Equal(AppLanguage.English, language);
    }

    [Fact]
    public void LocalizedText_contains_korean_and_english_ui_labels()
    {
        var english = LocalizedText.For(AppLanguage.English);
        var korean = LocalizedText.For(AppLanguage.Korean);

        Assert.Equal("Clear Words", english.ClearWords);
        Assert.Equal("단어 지우기", korean.ClearWords);
        Assert.Equal("Show Live Captions", english.ShowLiveCaptions);
        Assert.Equal("라이브 캡션 표시", korean.ShowLiveCaptions);
        Assert.Equal("Stack on windows", english.StackOnWindows);
        Assert.Equal("창 위에 쌓기", korean.StackOnWindows);
        Assert.Equal("Caption delay (ms)", english.CaptionDelayMilliseconds);
        Assert.Equal("캡션 안정화 지연(ms)", korean.CaptionDelayMilliseconds);
        Assert.Equal("Apply and Save", english.ApplyAndSave);
        Assert.Equal("적용 및 저장", korean.ApplyAndSave);
        Assert.Equal("Turn On", english.TurnOn);
        Assert.Equal("켜기", korean.TurnOn);
        Assert.Equal("Turn Off", english.TurnOff);
        Assert.Equal("끄기", korean.TurnOff);
        Assert.Equal("(current: 3 shown)", english.FormatCurrentWordCount(3));
        Assert.Equal("(현재: 3개 표시중)", korean.FormatCurrentWordCount(3));
        Assert.Equal("Left to right", english.SpawnModeLeftToRight);
        Assert.Equal("왼쪽에서 오른쪽", korean.SpawnModeLeftToRight);
    }
}
