using System.Globalization;

namespace CateringSaaS.Modules.Reporting.Services;

public static class ReportL10n
{
    public static string T(string en, string uk, string pl, string ru)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return culture switch
        {
            "uk" => uk,
            "pl" => pl,
            "ru" => ru,
            _ => en
        };
    }
}
