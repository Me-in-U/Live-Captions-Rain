namespace LiveCaptionsRain.Core.Captions;

public sealed class CaptionWordPipeline(
    double itemsPerSecond = 120,
    int maxItemsPerFrame = 8,
    TimeSpan? stabilizationDelay = null)
{
    private readonly WordSpawnQueue _queue = new(itemsPerSecond, maxItemsPerFrame);
    private readonly int _largeBurstThreshold = Math.Max(16, maxItemsPerFrame * 2);
    private readonly int _initialReplayThreshold = Math.Max(16, maxItemsPerFrame * 2);
    private readonly List<PendingWord> _pendingWords = [];
    private TimeSpan _stabilizationDelay = NormalizeDelay(stabilizationDelay ?? TimeSpan.Zero);
    private IReadOnlyList<string> _acceptedWords = [];
    private bool _suppressNextNonEmptyCaption;
    private bool _hasObservedNonEmptyCaption;

    public int PendingCount => _queue.Count;

    public void SetStabilizationDelay(TimeSpan delay)
    {
        _stabilizationDelay = NormalizeDelay(delay);
    }

    public void ObserveExisting(string? caption)
    {
        var words = CaptionTokenizer.Tokenize(caption);
        _queue.Clear();
        _pendingWords.Clear();
        ObserveAcceptedWords(words);
        if (words.Count > 0)
        {
            _hasObservedNonEmptyCaption = true;
        }

        _suppressNextNonEmptyCaption = words.Count == 0 && !_hasObservedNonEmptyCaption;
    }

    public void AddCaptionUpdate(string? caption)
    {
        AddCaptionUpdate(caption, TimeSpan.Zero);
    }

    public void AddCaptionUpdate(string? caption, TimeSpan now)
    {
        var currentWords = CaptionTokenizer.Tokenize(caption);
        if (currentWords.Count == 0)
        {
            return;
        }

        if (_suppressNextNonEmptyCaption && currentWords.Count > 0)
        {
            _suppressNextNonEmptyCaption = false;
            if (currentWords.Count > _initialReplayThreshold)
            {
                AbsorbCaptionUpdate(caption);
                return;
            }
        }

        _hasObservedNonEmptyCaption = true;
        var consumed = FindConsumedPrefixLength(_acceptedWords, currentWords);
        var newWords = currentWords.Skip(consumed).ToArray();

        if (newWords.Length > _largeBurstThreshold)
        {
            _queue.Clear();
            _pendingWords.Clear();
            ObserveAcceptedWords(currentWords);
            return;
        }

        UpdatePendingWords(newWords, now);
        FlushStablePendingWords(now);
    }

    public void AbsorbCaptionUpdate(string? caption)
    {
        var words = CaptionTokenizer.Tokenize(caption);
        _queue.Clear();
        _pendingWords.Clear();
        ObserveAcceptedWords(words);
        if (words.Count > 0)
        {
            _hasObservedNonEmptyCaption = true;
        }
    }

    public void ResumeEmitting()
    {
        _suppressNextNonEmptyCaption = false;
    }

    public IReadOnlyList<string> Drain(double deltaSeconds)
    {
        return _queue.Drain(deltaSeconds);
    }

    public void Clear(string? currentCaption)
    {
        ObserveExisting(currentCaption);
    }

    private void ObserveAcceptedWords(IReadOnlyList<string> words)
    {
        if (words.Count > 0 || _acceptedWords.Count == 0)
        {
            _acceptedWords = words.ToArray();
        }
    }

    private void UpdatePendingWords(IReadOnlyList<string> words, TimeSpan now)
    {
        var common = 0;
        var max = Math.Min(_pendingWords.Count, words.Count);
        while (common < max && StringComparer.Ordinal.Equals(_pendingWords[common].Text, words[common]))
        {
            common++;
        }

        if (_pendingWords.Count > common)
        {
            _pendingWords.RemoveRange(common, _pendingWords.Count - common);
        }

        for (var index = common; index < words.Count; index++)
        {
            _pendingWords.Add(new PendingWord(words[index], now));
        }
    }

    private void FlushStablePendingWords(TimeSpan now)
    {
        var readyCount = 0;
        while (readyCount < _pendingWords.Count && now - _pendingWords[readyCount].FirstSeenAt >= _stabilizationDelay)
        {
            readyCount++;
        }

        if (readyCount == 0)
        {
            return;
        }

        var readyWords = _pendingWords.Take(readyCount).Select(word => word.Text).ToArray();
        _queue.Enqueue(readyWords);
        _acceptedWords = TrimAcceptedWords(_acceptedWords.Concat(readyWords).ToArray());
        _pendingWords.RemoveRange(0, readyCount);
    }

    private static IReadOnlyList<string> TrimAcceptedWords(IReadOnlyList<string> words)
    {
        const int maxAcceptedWords = 160;
        return words.Count <= maxAcceptedWords
            ? words.ToArray()
            : words.Skip(words.Count - maxAcceptedWords).ToArray();
    }

    private static TimeSpan NormalizeDelay(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var maxDelay = TimeSpan.FromSeconds(5);
        return delay > maxDelay ? maxDelay : delay;
    }

    private static int FindConsumedPrefixLength(IReadOnlyList<string> previous, IReadOnlyList<string> current)
    {
        var embeddedTail = FindLargestPreviousSuffixInsideCurrent(previous, current);
        if (embeddedTail > 0)
        {
            return embeddedTail;
        }

        return FindLargestSuffixPrefixOverlap(previous, current);
    }

    private static int FindLargestPreviousSuffixInsideCurrent(IReadOnlyList<string> previous, IReadOnlyList<string> current)
    {
        var max = Math.Min(previous.Count, current.Count);
        for (var length = max; length > 0; length--)
        {
            var previousStart = previous.Count - length;
            for (var currentStart = current.Count - length; currentStart >= 0; currentStart--)
            {
                if (Matches(previous, previousStart, current, currentStart, length))
                {
                    return currentStart + length;
                }
            }
        }

        return 0;
    }

    private static int FindLargestSuffixPrefixOverlap(IReadOnlyList<string> previous, IReadOnlyList<string> current)
    {
        var max = Math.Min(previous.Count, current.Count);
        for (var length = max; length > 0; length--)
        {
            if (Matches(previous, previous.Count - length, current, 0, length))
            {
                return length;
            }
        }

        return 0;
    }

    private static bool Matches(
        IReadOnlyList<string> left,
        int leftStart,
        IReadOnlyList<string> right,
        int rightStart,
        int length)
    {
        for (var index = 0; index < length; index++)
        {
            if (!StringComparer.Ordinal.Equals(left[leftStart + index], right[rightStart + index]))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record PendingWord(string Text, TimeSpan FirstSeenAt);
}
