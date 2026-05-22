using LiveCaptionsRain.Core.Text;

namespace LiveCaptionsRain.Tests;

public sealed class HangulFragmenterTests
{
    [Fact]
    public void DecomposeSyllable_returns_compatibility_jamo_for_complete_hangul()
    {
        Assert.Equal(["ㅇ", "ㅏ", "ㄴ"], HangulFragmenter.DecomposeSyllable('안'));
        Assert.Equal(["ㅎ", "ㅏ"], HangulFragmenter.DecomposeSyllable('하'));
    }

    [Fact]
    public void DecomposeForBurn_removes_vowels_first_then_syllable_shape()
    {
        Assert.Equal("ㅇㄴㅎㅅㅇ", HangulFragmenter.DecomposeForBurn("안녕하세요"));
    }
}
