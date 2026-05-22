using LiveCaptionsRain.Core.Captions;

namespace LiveCaptionsRain.Tests;

public sealed class WordSpawnQueueTests
{
    [Fact]
    public void Drain_consumes_fast_but_caps_each_frame()
    {
        var queue = new WordSpawnQueue(itemsPerSecond: 120, maxItemsPerFrame: 8);
        queue.Enqueue(Enumerable.Range(0, 20).Select(index => $"w{index}"));

        var drained = queue.Drain(deltaSeconds: 1);

        Assert.Equal(8, drained.Count);
        Assert.Equal(12, queue.Count);
    }

    [Fact]
    public void Drain_uses_elapsed_time_for_fast_frame_consumption()
    {
        var queue = new WordSpawnQueue(itemsPerSecond: 120, maxItemsPerFrame: 8);
        queue.Enqueue(["one", "two", "three", "four"]);

        var drained = queue.Drain(deltaSeconds: 1d / 60d);

        Assert.Equal(["one", "two"], drained);
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void Clear_removes_pending_words_and_prevents_leftover_budget_burst()
    {
        var queue = new WordSpawnQueue(itemsPerSecond: 120, maxItemsPerFrame: 8);
        queue.Enqueue(["one", "two", "three"]);
        _ = queue.Drain(deltaSeconds: 1);

        queue.Enqueue(["four", "five"]);
        queue.Clear();
        queue.Enqueue(["six"]);

        Assert.Equal(["six"], queue.Drain(deltaSeconds: 1d / 60d));
        Assert.Equal(0, queue.Count);
    }
}
