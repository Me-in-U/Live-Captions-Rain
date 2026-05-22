using System.Globalization;

namespace LiveCaptionsRain.Core.Localization;

public static class AppLanguageResolver
{
    public static AppLanguage Resolve(CultureInfo culture)
    {
        return culture.TwoLetterISOLanguageName.Equals("ko", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Korean
            : AppLanguage.English;
    }
}
