using System.Globalization;
using System.Text;

namespace LiveCaptionsRain.Core.Captions;

public static class CaptionTokenizer
{
    public static IReadOnlyList<string> Tokenize(string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption))
        {
            return [];
        }

        var words = new List<string>();
        var current = new StringBuilder();
        var enumerator = StringInfo.GetTextElementEnumerator(caption);

        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            if (IsWordElement(element))
            {
                current.Append(element);
                continue;
            }

            Flush(words, current);
        }

        Flush(words, current);
        return words;
    }

    private static bool IsWordElement(string element)
    {
        foreach (var rune in element.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter
                or UnicodeCategory.DecimalDigitNumber
                or UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark)
            {
                return true;
            }
        }

        return false;
    }

    private static void Flush(List<string> words, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        words.Add(current.ToString());
        current.Clear();
    }
}
