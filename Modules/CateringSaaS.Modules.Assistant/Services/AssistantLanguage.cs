using System.Globalization;

namespace CateringSaaS.Modules.Assistant.Services;

internal static class AssistantLanguage
{
    private static readonly char[] UkrainianMarks = ['і', 'І', 'ї', 'Ї', 'є', 'Є', 'ґ', 'Ґ'];
    private static readonly char[] PolishMarks =
        ['ą', 'ć', 'ę', 'ł', 'ń', 'ó', 'ś', 'ź', 'ż', 'Ą', 'Ć', 'Ę', 'Ł', 'Ń', 'Ó', 'Ś', 'Ź', 'Ż'];

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

        return Normalize(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
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
