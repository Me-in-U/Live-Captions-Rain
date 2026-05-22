using System.Globalization;
using System.Text;

namespace LiveCaptionsRain.Core.Text;

public static class UnicodeTextElementFragmenter
{
    public static IReadOnlyList<string> DecomposeTextElement(string element)
    {
        if (string.IsNullOrEmpty(element))
        {
            return [];
        }

        if (element.Length == 1 && HangulFragmenter.IsHangulSyllable(element[0]))
        {
            return HangulFragmenter.DecomposeSyllable(element[0]);
        }

        var normalized = element.Normalize(NormalizationForm.FormD);
        var codePoints = normalized.EnumerateRunes().ToArray();
        if (codePoints.Length <= 1 || !IsSupportedStarter(codePoints[0]))
        {
            return [element];
        }

        if (codePoints.Skip(1).Any(codePoint => !IsCombiningMark(codePoint)))
        {
            return [element];
        }

        return codePoints.Select(ToStandaloneFragment).ToArray();
    }

    public static IReadOnlyList<string> DecomposeTextElements(IEnumerable<string> elements)
    {
        var fragments = new List<string>();
        foreach (var element in elements)
        {
            fragments.AddRange(DecomposeTextElement(element));
        }

        return fragments;
    }

    private static bool IsSupportedStarter(Rune codePoint)
    {
        return IsLatin(codePoint)
            || IsGreek(codePoint)
            || IsCyrillic(codePoint)
            || IsJapaneseKana(codePoint);
    }

    private static bool IsLatin(Rune codePoint)
    {
        return IsInRange(codePoint, 0x0041, 0x024F)
            || IsInRange(codePoint, 0x1E00, 0x1EFF)
            || IsInRange(codePoint, 0xA720, 0xA7FF);
    }

    private static bool IsGreek(Rune codePoint)
    {
        return IsInRange(codePoint, 0x0370, 0x03FF)
            || IsInRange(codePoint, 0x1F00, 0x1FFF);
    }

    private static bool IsCyrillic(Rune codePoint)
    {
        return IsInRange(codePoint, 0x0400, 0x052F)
            || IsInRange(codePoint, 0x1C80, 0x1C8F)
            || IsInRange(codePoint, 0x2DE0, 0x2DFF)
            || IsInRange(codePoint, 0xA640, 0xA69F);
    }

    private static bool IsJapaneseKana(Rune codePoint)
    {
        return IsInRange(codePoint, 0x3040, 0x30FF)
            || IsInRange(codePoint, 0xFF66, 0xFF9F);
    }

    private static bool IsInRange(Rune codePoint, int minInclusive, int maxInclusive)
    {
        return codePoint.Value >= minInclusive && codePoint.Value <= maxInclusive;
    }

    private static bool IsCombiningMark(Rune codePoint)
    {
        var category = Rune.GetUnicodeCategory(codePoint);
        return category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;
    }

    private static string ToStandaloneFragment(Rune codePoint)
    {
        return codePoint.Value switch
        {
            0x0300 => "ˋ",
            0x0301 => "´",
            0x0302 => "ˆ",
            0x0303 => "˜",
            0x0304 => "¯",
            0x0306 => "˘",
            0x0307 => "˙",
            0x0308 => "¨",
            0x030A => "˚",
            0x030B => "˝",
            0x030C => "ˇ",
            0x0327 => "¸",
            0x0328 => "˛",
            0x3099 => "゛",
            0x309A => "゜",
            _ => codePoint.ToString()
        };
    }
}
