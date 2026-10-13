using System.Globalization;
using System.Text.RegularExpressions;

namespace CateringSaaS.Modules.Assistant.Services;

internal static class AssistantLanguage
{
    private static readonly char[] UkrainianMarks = ['і', 'І', 'ї', 'Ї', 'є', 'Є', 'ґ', 'Ґ'];
    private static readonly char[] PolishMarks =
        ['ą', 'ć', 'ę', 'ł', 'ń', 'ó', 'ś', 'ź', 'ż', 'Ą', 'Ć', 'Ę', 'Ł', 'Ń', 'Ó', 'Ś', 'Ź', 'Ż'];

    /// <summary>
    /// Common Polish tokens that often appear without diacritics in chat
    /// (e.g. "Jaki jest puls…", "ostatnie 7 dni").
    /// </summary>
    private static readonly Regex PolishWordHints = new(
        """
        \b(?:
        jaki|jaka|jakie|jakich|jakiego|jakiej|
        jest|są|czy|dla|przez|oraz|albo|
        ile|pokaz|pokazac|pokaż|pokażcie|
        ostatni|ostatnia|ostatnie|ostatnich|
        dzien|dzień|dni|tydzień|tydzien|tygodnie|miesiac|miesiąc|rok|roku|
        dzisiaj|wczoraj|
        zamówienia|zamowienia|przychód|przychod|koszty|dostawa|dostawy|
        operacyjny|operacyjna|operacyjne|puls|podsumowanie
        )\b
        """,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    /// <summary>
    /// Detect reply language from the user message. DB field values stay as stored;
    /// this only drives chat prose + CurrentUICulture for report labels.
    /// </summary>
    public static string Detect(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Normalize(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        }

        var text = message.AsSpan().Trim();
        if (text.IndexOfAny(PolishMarks) >= 0)
        {
            return "pl";
        }

        if (text.IndexOfAny(UkrainianMarks) >= 0)
        {
            return "uk";
        }

        foreach (var c in text)
        {
            if (c is (>= 'а' and <= 'я') or (>= 'А' and <= 'Я') or 'ё' or 'Ё')
            {
                return "ru";
            }
        }

        // Latin without diacritics: still detect Polish from common function words.
        if (PolishWordHints.IsMatch(message))
        {
            return "pl";
        }

        // Latin without PL/UK markers → English. Never fall back to Accept-Language /
        // CurrentUICulture here: that is the shell locale, not this chat turn's language.
        return "en";
    }

    public static string DisplayName(string lang) => lang switch
    {
        "uk" => "Ukrainian",
        "pl" => "Polish",
        "ru" => "Russian",
        _ => "English"
    };

    public static CultureInfo ToCulture(string lang) =>
        CultureInfo.GetCultureInfo(Normalize(lang) switch
        {
            "uk" => "uk-UA",
            "pl" => "pl-PL",
            "ru" => "ru-RU",
            _ => "en-US"
        });

    private static string Normalize(string lang) => lang.ToLowerInvariant() switch
    {
        "uk" or "ua" => "uk",
        "pl" => "pl",
        "ru" => "ru",
        _ => "en"
    };
}

internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _previousCulture;
    private readonly CultureInfo _previousUiCulture;

    public CultureScope(string language)
    {
        _previousCulture = CultureInfo.CurrentCulture;
        _previousUiCulture = CultureInfo.CurrentUICulture;
        var culture = AssistantLanguage.ToCulture(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }
}
