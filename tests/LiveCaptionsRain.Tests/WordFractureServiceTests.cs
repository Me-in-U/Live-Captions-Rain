using LiveCaptionsRain.Core.Text;

namespace LiveCaptionsRain.Tests;

public sealed class WordFractureServiceTests
{
    [Fact]
    public void Fracture_splits_latin_word_at_hit_position()
    {
        var fragments = WordFractureService.Fracture("apple", 0.6);

        Assert.Equal(["app", "le"], fragments);
    }

    [Fact]
    public void Fracture_decomposes_hit_hangul_syllable_and_keeps_sides()
    {
        var fragments = WordFractureService.Fracture("안녕하세요", 0.5);

        Assert.Equal(["안녕", "ㅎ", "ㅏ", "세요"], fragments);
    }

    [Fact]
    public void Fracture_decomposes_latin_precomposed_marks_and_keeps_sides()
    {
        var fragments = WordFractureService.Fracture("mắt", 0.5);

        Assert.Equal(["m", "a", "˘", "´", "t"], fragments);
    }

    [Fact]
    public void Fracture_decomposes_greek_and_cyrillic_precomposed_marks()
    {
        Assert.Equal(["α", "´"], WordFractureService.Fracture("ά", 0.5));
        Assert.Equal(["и", "˘"], WordFractureService.Fracture("й", 0.5));
    }

    [Fact]
    public void Fracture_decomposes_japanese_kana_voice_marks()
    {
        Assert.Equal(["か", "゛"], WordFractureService.Fracture("が", 0.5));
        Assert.Equal(["ハ", "゜"], WordFractureService.Fracture("パ", 0.5));
    }

    [Fact]
    public void Fracture_clamps_edges_to_useful_fragments()
    {
        Assert.Equal(["a", "pple"], WordFractureService.Fracture("apple", 0));
        Assert.Equal(["appl", "e"], WordFractureService.Fracture("apple", 1));
    }

    [Fact]
    public void FractureRandomSegments_creates_ordered_multiple_non_empty_fragments()
    {
        var fragments = WordFractureService.FractureRandomSegments("captioned", new Random(5));

        Assert.InRange(fragments.Count, 3, 5);
        Assert.Equal("captioned", string.Concat(fragments));
        Assert.All(fragments, fragment => Assert.False(string.IsNullOrEmpty(fragment)));
    }

    [Fact]
    public void FractureRandomSegments_keeps_korean_syllables_for_multi_syllable_words()
    {
        var fragments = WordFractureService.FractureRandomSegments("안녕하세요", new Random(5));

        Assert.InRange(fragments.Count, 3, 5);
        Assert.Equal("안녕하세요", string.Concat(fragments));
    }

    [Fact]
    public void FractureRandomSegments_uses_decomposed_units_for_supported_scripts()
    {
        Assert.Equal(["a", "˘", "´"], WordFractureService.FractureRandomSegments("ắ", new Random(5)));
        Assert.Equal(["か", "゛"], WordFractureService.FractureRandomSegments("が", new Random(5)));
    }

    [Fact]
    public void FractureRandomSegments_decomposes_already_normalized_mark_sequences()
    {
        Assert.Equal(["a", "˘", "´"], WordFractureService.FractureRandomSegments("a\u0306\u0301", new Random(5)));
    }
}
