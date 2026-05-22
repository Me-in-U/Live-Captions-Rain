using System;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace LiveCaptionsRain;

internal static class FontFamilyResolver
{
    private static readonly string[] FallbackFamilies =
    [
        "Malgun Gothic",
        "Segoe UI",
        "Segoe UI Emoji",
        "Segoe UI Symbol"
    ];

    public static FontFamily Create(string? preferredFamily)
    {
        var families = new[] { preferredFamily }
            .Concat(FallbackFamilies)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return new FontFamily(string.Join(", ", families));
    }

    public static FontOption[] GetSystemFonts(CultureInfo culture)
    {
        var cultureKey = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
        var englishKey = XmlLanguage.GetLanguage("en-US");
        return Fonts.SystemFontFamilies
            .Where(family => !family.Source.StartsWith('@'))
            .Select(family => new FontOption(
                GetDisplayName(family, cultureKey, englishKey),
                family.Source))
            .GroupBy(option => option.Display, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => group.First())
            .OrderBy(option => option.Display)
            .ToArray();
    }

    private static string GetDisplayName(FontFamily family, XmlLanguage cultureKey, XmlLanguage englishKey)
    {
        if (family.FamilyNames.TryGetValue(cultureKey, out var localized))
        {
            return localized;
        }

        if (family.FamilyNames.TryGetValue(englishKey, out var english))
        {
            return english;
        }

        return family.Source;
    }
}

internal sealed record FontOption(string Display, string Source)
{
    public override string ToString() => Display;
}
