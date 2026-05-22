using LiveCaptionsRain.Core.Captions;

namespace LiveCaptionsRain.Tests;

public sealed class CaptionTokenizerTests
{
    [Fact]
    public void Tokenize_keeps_english_numbers_and_korean_words()
    {
        var words = CaptionTokenizer.Tokenize("Hello, Windows 11! 안녕하세요 live-caption.");

        Assert.Equal(["Hello", "Windows", "11", "안녕하세요", "live", "caption"], words);
    }

    [Fact]
    public void Tokenize_ignores_repeated_spacing_and_punctuation()
    {
        var words = CaptionTokenizer.Tokenize("  apple...  바나나\torange?  ");

        Assert.Equal(["apple", "바나나", "orange"], words);
    }
}
