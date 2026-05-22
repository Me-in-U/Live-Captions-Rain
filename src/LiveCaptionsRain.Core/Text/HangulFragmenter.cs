namespace LiveCaptionsRain.Core.Text;

public static class HangulFragmenter
{
    private const int SBase = 0xAC00;
    private const int LCount = 19;
    private const int VCount = 21;
    private const int TCount = 28;
    private const int NCount = VCount * TCount;
    private const int SCount = LCount * NCount;

    private static readonly string[] Leading =
    [
        "ㄱ", "ㄲ", "ㄴ", "ㄷ", "ㄸ", "ㄹ", "ㅁ", "ㅂ", "ㅃ", "ㅅ",
        "ㅆ", "ㅇ", "ㅈ", "ㅉ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ"
    ];

    private static readonly string[] Vowels =
    [
        "ㅏ", "ㅐ", "ㅑ", "ㅒ", "ㅓ", "ㅔ", "ㅕ", "ㅖ", "ㅗ", "ㅘ",
        "ㅙ", "ㅚ", "ㅛ", "ㅜ", "ㅝ", "ㅞ", "ㅟ", "ㅠ", "ㅡ", "ㅢ", "ㅣ"
    ];

    private static readonly string[] Trailing =
    [
        "", "ㄱ", "ㄲ", "ㄳ", "ㄴ", "ㄵ", "ㄶ", "ㄷ", "ㄹ", "ㄺ",
        "ㄻ", "ㄼ", "ㄽ", "ㄾ", "ㄿ", "ㅀ", "ㅁ", "ㅂ", "ㅄ", "ㅅ",
        "ㅆ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ"
    ];

    public static IReadOnlyList<string> DecomposeSyllable(char syllable)
    {
        if (!TryGetIndexes(syllable, out var leadingIndex, out var vowelIndex, out var trailingIndex))
        {
            return [syllable.ToString()];
        }

        var parts = new List<string> { Leading[leadingIndex], Vowels[vowelIndex] };
        if (trailingIndex > 0)
        {
            parts.Add(Trailing[trailingIndex]);
        }

        return parts;
    }

    public static string DecomposeForBurn(string text)
    {
        var result = new List<string>();
        foreach (var ch in text)
        {
            if (TryGetIndexes(ch, out var leadingIndex, out _, out _))
            {
                result.Add(Leading[leadingIndex]);
            }
            else if (!char.IsWhiteSpace(ch))
            {
                result.Add(ch.ToString());
            }
        }

        return string.Concat(result);
    }

    public static bool IsHangulSyllable(char value)
    {
        var index = value - SBase;
        return index >= 0 && index < SCount;
    }

    private static bool TryGetIndexes(char syllable, out int leadingIndex, out int vowelIndex, out int trailingIndex)
    {
        var syllableIndex = syllable - SBase;
        if (syllableIndex < 0 || syllableIndex >= SCount)
        {
            leadingIndex = 0;
            vowelIndex = 0;
            trailingIndex = 0;
            return false;
        }

        leadingIndex = syllableIndex / NCount;
        vowelIndex = syllableIndex % NCount / TCount;
        trailingIndex = syllableIndex % TCount;
        return true;
    }
}
