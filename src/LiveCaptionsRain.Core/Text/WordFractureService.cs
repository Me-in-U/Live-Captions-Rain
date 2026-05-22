using System.Globalization;

namespace LiveCaptionsRain.Core.Text;

public static class WordFractureService
{
    private const int PreferredRandomFragmentCount = 3;
    private const int MaxRandomFragmentCount = 5;

    public static IReadOnlyList<string> Fracture(string text, double hitRatio)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var elements = GetTextElements(text);
        if (elements.Count <= 1)
        {
            return TryDecomposeSingleElement(elements[0]);
        }

        var hitIndex = Math.Clamp((int)Math.Floor(elements.Count * Math.Clamp(hitRatio, 0, 0.999999)), 0, elements.Count - 1);
        var decomposedHitElement = UnicodeTextElementFragmenter.DecomposeTextElement(elements[hitIndex]);
        if (IsDecomposed(elements[hitIndex], decomposedHitElement))
        {
            var fragments = new List<string>();
            AddIfNotEmpty(fragments, string.Concat(elements.Take(hitIndex)));
            fragments.AddRange(decomposedHitElement);
            AddIfNotEmpty(fragments, string.Concat(elements.Skip(hitIndex + 1)));
            return fragments;
        }

        var splitIndex = Math.Clamp(
            (int)Math.Round(elements.Count * Math.Clamp(hitRatio, 0, 1), MidpointRounding.AwayFromZero),
            1,
            elements.Count - 1);

        return [string.Concat(elements.Take(splitIndex)), string.Concat(elements.Skip(splitIndex))];
    }

    public static IReadOnlyList<string> FractureRandomSegments(string text, Random random)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var elements = UnicodeTextElementFragmenter.DecomposeTextElements(GetTextElements(text));
        if (elements.Count <= 1)
        {
            return elements;
        }

        var maxFragments = Math.Min(MaxRandomFragmentCount, elements.Count);
        var minFragments = Math.Min(PreferredRandomFragmentCount, maxFragments);
        var fragmentCount = random.Next(minFragments, maxFragments + 1);
        var cuts = Enumerable
            .Range(1, elements.Count - 1)
            .OrderBy(_ => random.Next())
            .Take(fragmentCount - 1)
            .Order()
            .ToArray();

        var fragments = new List<string>(fragmentCount);
        var start = 0;
        foreach (var cut in cuts)
        {
            fragments.Add(string.Concat(elements.Skip(start).Take(cut - start)));
            start = cut;
        }

        fragments.Add(string.Concat(elements.Skip(start)));
        return fragments;
    }

    private static IReadOnlyList<string> TryDecomposeSingleElement(string element)
    {
        return UnicodeTextElementFragmenter.DecomposeTextElement(element);
    }

    private static List<string> GetTextElements(string text)
    {
        var elements = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.GetTextElement());
        }

        return elements;
    }

    private static void AddIfNotEmpty(List<string> values, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            values.Add(value);
        }
    }

    private static bool IsDecomposed(string original, IReadOnlyList<string> fragments)
    {
        return fragments.Count != 1 || !string.Equals(fragments[0], original, StringComparison.Ordinal);
    }
}
