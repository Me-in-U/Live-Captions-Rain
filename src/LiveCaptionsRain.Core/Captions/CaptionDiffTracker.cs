namespace LiveCaptionsRain.Core.Captions;

public sealed class CaptionDiffTracker
{
    private IReadOnlyList<string> _previousWords = [];

    public IReadOnlyList<string> ExtractNewWords(string? caption)
    {
        var currentWords = CaptionTokenizer.Tokenize(caption);
        if (currentWords.Count == 0)
        {
            return [];
        }

        var consumed = FindConsumedPrefixLength(_previousWords, currentWords);
        var newWords = currentWords.Skip(consumed).ToArray();
        _previousWords = currentWords;
        return newWords;
    }

    public void Reset()
    {
        _previousWords = [];
    }

    public void ObserveWithoutEmitting(string? caption)
    {
        var words = CaptionTokenizer.Tokenize(caption);
        if (words.Count > 0 || _previousWords.Count == 0)
        {
            _previousWords = words;
        }
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
}
