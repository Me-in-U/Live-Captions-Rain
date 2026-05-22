namespace LiveCaptionsRain.Core.Captions;

public sealed class WordSpawnQueue(double itemsPerSecond = 120, int maxItemsPerFrame = 8)
{
    private readonly Queue<string> _words = [];
    private readonly double _itemsPerSecond = Math.Max(1, itemsPerSecond);
    private readonly int _maxItemsPerFrame = Math.Max(1, maxItemsPerFrame);
    private double _budget;

    public int Count => _words.Count;

    public void Enqueue(IEnumerable<string> words)
    {
        foreach (var word in words)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                _words.Enqueue(word);
            }
        }
    }

    public IReadOnlyList<string> Drain(double deltaSeconds)
    {
        if (_words.Count == 0)
        {
            _budget = 0;
            return [];
        }

        _budget += Math.Clamp(deltaSeconds, 0, 0.25d) * _itemsPerSecond;
        var take = Math.Min(_words.Count, Math.Min(_maxItemsPerFrame, (int)Math.Floor(_budget)));
        if (take <= 0)
        {
            return [];
        }

        _budget -= take;
        var drained = new List<string>(take);
        for (var index = 0; index < take; index++)
        {
            drained.Add(_words.Dequeue());
        }

        return drained;
    }

    public void Clear()
    {
        _words.Clear();
        _budget = 0;
    }
}
