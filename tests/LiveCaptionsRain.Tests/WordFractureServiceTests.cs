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
}
