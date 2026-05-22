using LiveCaptionsRain.Core.Captions;

namespace LiveCaptionsRain.Tests;

public sealed class CaptionDiffTrackerTests
{
    [Fact]
    public void ExtractNewWords_returns_initial_caption_words_once()
    {
        var tracker = new CaptionDiffTracker();

        var first = tracker.ExtractNewWords("hello world hello");
        var duplicate = tracker.ExtractNewWords("hello world hello");

        Assert.Equal(["hello", "world", "hello"], first);
        Assert.Empty(duplicate);
    }

    [Fact]
    public void ExtractNewWords_uses_overlap_when_live_caption_window_slides()
    {
        var tracker = new CaptionDiffTracker();

        tracker.ExtractNewWords("hello world apple");
        var words = tracker.ExtractNewWords("world apple banana");

        Assert.Equal(["banana"], words);
    }

    [Fact]
    public void Reset_allows_same_words_to_spawn_again()
    {
        var tracker = new CaptionDiffTracker();

        tracker.ExtractNewWords("다시 시작");
        tracker.Reset();

        Assert.Equal(["다시", "시작"], tracker.ExtractNewWords("다시 시작"));
    }

    [Fact]
    public void ObserveWithoutEmitting_skips_current_caption_after_clear()
    {
        var tracker = new CaptionDiffTracker();

        tracker.ObserveWithoutEmitting("hello world");

        Assert.Empty(tracker.ExtractNewWords("hello world"));
        Assert.Equal(["again"], tracker.ExtractNewWords("hello world again"));
    }

    [Fact]
    public void ObserveWithoutEmitting_keeps_previous_caption_when_clear_reads_empty_transiently()
    {
        var tracker = new CaptionDiffTracker();

        tracker.ExtractNewWords("hello world");
        tracker.ObserveWithoutEmitting("");

        Assert.Empty(tracker.ExtractNewWords("hello world"));
        Assert.Equal(["again"], tracker.ExtractNewWords("hello world again"));
    }

    [Fact]
    public void ExtractNewWords_keeps_previous_caption_when_update_reads_empty_transiently()
    {
        var tracker = new CaptionDiffTracker();

        tracker.ExtractNewWords("hello world");

        Assert.Empty(tracker.ExtractNewWords(""));
        Assert.Empty(tracker.ExtractNewWords("hello world"));
        Assert.Equal(["again"], tracker.ExtractNewWords("hello world again"));
    }

    [Fact]
    public void ExtractNewWords_uses_previous_tail_even_when_live_captions_prepends_history()
    {
        var tracker = new CaptionDiffTracker();

        tracker.ObserveWithoutEmitting("지금 말");
        var words = tracker.ExtractNewWords("예전 기록 단어 지금 말 새단어");

        Assert.Equal(["새단어"], words);
    }
}
