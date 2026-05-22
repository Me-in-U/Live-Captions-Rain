using LiveCaptionsRain.Core.Captions;

namespace LiveCaptionsRain.Tests;

public sealed class CaptionWordPipelineTests
{
    [Fact]
    public void ObserveExisting_prevents_startup_caption_dump()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);

        pipeline.ObserveExisting("old words already visible");
        pipeline.AddCaptionUpdate("old words already visible");

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate("old words already visible new");

        Assert.Equal(["new"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void Clear_removes_pending_words_and_keeps_last_caption_when_current_read_is_empty()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);

        pipeline.ObserveExisting("old words");
        pipeline.AddCaptionUpdate("old words one two three four five six seven eight nine");
        pipeline.Clear("");
        pipeline.AddCaptionUpdate("old words one two three four five six seven eight nine");

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate("old words one two three four five six seven eight nine ten");

        Assert.Equal(["ten"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void Clear_with_empty_read_preserves_existing_baseline_for_next_caption()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);

        pipeline.ObserveExisting("old words");
        pipeline.AddCaptionUpdate("old words one two");
        _ = pipeline.Drain(deltaSeconds: 1);

        pipeline.Clear("");
        pipeline.AddCaptionUpdate("old words one two fresh");

        Assert.Equal(["fresh"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void First_short_caption_after_empty_startup_spawns_immediately()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);

        pipeline.ObserveExisting("");
        pipeline.AddCaptionUpdate("fresh words");

        Assert.Equal(["fresh", "words"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void AddCaptionUpdate_delays_new_words_until_stable()
    {
        var pipeline = new CaptionWordPipeline(
            itemsPerSecond: 120,
            maxItemsPerFrame: 8,
            stabilizationDelay: TimeSpan.FromMilliseconds(700));

        pipeline.ObserveExisting("already");
        pipeline.AddCaptionUpdate("already fresh", TimeSpan.Zero);

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate("already fresh", TimeSpan.FromMilliseconds(699));

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate("already fresh", TimeSpan.FromMilliseconds(700));

        Assert.Equal(["fresh"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void AddCaptionUpdate_replaces_unstable_recognition_before_delay()
    {
        var pipeline = new CaptionWordPipeline(
            itemsPerSecond: 120,
            maxItemsPerFrame: 8,
            stabilizationDelay: TimeSpan.FromMilliseconds(700));

        pipeline.ObserveExisting("i like");
        pipeline.AddCaptionUpdate("i like appel", TimeSpan.Zero);
        pipeline.AddCaptionUpdate("i like apple", TimeSpan.FromMilliseconds(300));
        pipeline.AddCaptionUpdate("i like apple", TimeSpan.FromMilliseconds(999));

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate("i like apple", TimeSpan.FromMilliseconds(1000));

        Assert.Equal(["apple"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void AbsorbCaptionUpdate_consumes_late_history_without_emitting_old_words()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);

        pipeline.ObserveExisting("old");
        pipeline.AddCaptionUpdate("old one two three four five");
        pipeline.Clear("old");
        pipeline.AbsorbCaptionUpdate("old one two");
        pipeline.AbsorbCaptionUpdate("old one two three four five");
        pipeline.AddCaptionUpdate("old one two three four five new");

        Assert.Equal(["new"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void AbsorbCaptionUpdate_handles_large_history_burst_before_resuming_new_words()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);

        pipeline.ObserveExisting("tail");
        pipeline.Clear("unrelated");
        pipeline.AbsorbCaptionUpdate("one two three four five six seven eight old first");

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate("one two three four five six seven eight old first next");

        Assert.Equal(["next"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void ResumeEmitting_keeps_large_history_guard_for_first_non_empty_caption()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);
        var history = string.Join(' ', Enumerable.Range(1, 40).Select(index => $"old{index}"));

        pipeline.ObserveExisting("");
        pipeline.ResumeEmitting();
        pipeline.AddCaptionUpdate(history);

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate($"{history} fresh");

        Assert.Equal(["fresh"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void Large_history_burst_is_absorbed_once_then_later_large_delta_can_spawn()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);
        var history = string.Join(' ', Enumerable.Range(1, 40).Select(index => $"old{index}"));
        var newWords = Enumerable.Range(1, 12).Select(index => $"new{index}").ToArray();

        pipeline.ObserveExisting("tail");
        pipeline.Clear("unrelated");
        pipeline.AddCaptionUpdate(history);

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate($"{history} {string.Join(' ', newWords)}");

        var drained = pipeline.Drain(deltaSeconds: 1)
            .Concat(pipeline.Drain(deltaSeconds: 1))
            .ToArray();
        Assert.Equal(newWords, drained);
    }

    [Fact]
    public void AddCaptionUpdate_absorbs_large_replay_after_normal_emission()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 8);
        var history = string.Join(' ', Enumerable.Range(1, 80).Select(index => $"old{index}"));

        pipeline.ObserveExisting("live");
        pipeline.AddCaptionUpdate("live first");

        Assert.Equal(["first"], pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate(history);

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate($"{history} fresh");

        Assert.Equal(["fresh"], pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void AddCaptionUpdate_clears_pending_queue_when_large_replay_arrives()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 2);
        var history = string.Join(' ', Enumerable.Range(1, 80).Select(index => $"old{index}"));

        pipeline.ObserveExisting("live");
        pipeline.AddCaptionUpdate("live one two three four");

        Assert.Equal(["one", "two"], pipeline.Drain(deltaSeconds: 1));

        pipeline.AddCaptionUpdate(history);

        Assert.Empty(pipeline.Drain(deltaSeconds: 1));
    }

    [Fact]
    public void AddCaptionUpdate_emits_long_new_caption_after_resume()
    {
        var pipeline = new CaptionWordPipeline(itemsPerSecond: 120, maxItemsPerFrame: 20);

        pipeline.ObserveExisting("");
        pipeline.ResumeEmitting();
        pipeline.AddCaptionUpdate("one two three four five six seven eight nine ten");

        Assert.Equal(
            ["one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"],
            pipeline.Drain(deltaSeconds: 1));
    }
}
