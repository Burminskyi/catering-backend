using System.Globalization;

namespace CateringSaaS.Modules.Reporting.Services;

/// <summary>
/// Localized titles for the 12 operational reports, resolved from the current UI culture
/// (set by RequestLocalization from Accept-Language).
/// </summary>
internal static class ReportTitles
{
    public static string TodayPulse => T(
        en: "Today's operations pulse",
        uk: "Операційний пульс на сьогодні",
        pl: "Puls operacji na dziś",
        ru: "Операционный пульс на сегодня");

    public static string RevenueByClient => T(
        en: "Revenue & volume by client",
        uk: "Дохід і обсяг за клієнтами",
        pl: "Przychód i wolumen według klienta",
        ru: "Выручка и объём по клиентам");

    public static string StockMovements => T(
        en: "Stock movement summary",
        uk: "Підсумок руху складу",
        pl: "Podsumowanie ruchu magazynowego",
        ru: "Сводка движения склада");

    public static string DeliveryAudit => T(
        en: "Client delivery & reclamations audit",
        uk: "Аудит доставок і рекламацій",
        pl: "Audyt dostaw i reklamacji klientów",
        ru: "Аудит доставок и рекламаций");

    public static string DishPopularity => T(
        en: "Dish popularity & production volume",
        uk: "Популярність страв і обсяги виробництва",
        pl: "Popularność dań i wolumen produkcji",
        ru: "Популярность блюд и объём производства");

    public static string ReclamationHeatMap => T(
        en: "Reclamation heat map",
        uk: "Теплова карта рекламацій",
        pl: "Mapa ciepła reklamacji",
        ru: "Тепловая карта рекламаций");

    public static string ConsumptionVariance => T(
        en: "Ingredient consumption vs dish production",
        uk: "Витрата інгредієнтів vs виробництво страв",
        pl: "Zużycie składników vs produkcja dań",
        ru: "Расход ингредиентов vs производство блюд");

    public static string FoodCost => T(
        en: "Food-cost & usage efficiency",
        uk: "Food-cost і ефективність використання",
        pl: "Food-cost i efektywność zużycia",
        ru: "Food-cost и эффективность использования");

    public static string ShortageForecast => T(
        en: "Shopping & shortage forecast",
        uk: "Прогноз закупівель і дефіциту",
        pl: "Prognoza zakupów i braków",
        ru: "Прогноз закупок и дефицита");

    public static string DriverEfficiency => T(
        en: "Driver fulfillment & efficiency",
        uk: "Виконання та ефективність водіїв",
        pl: "Realizacja i efektywność kierowców",
        ru: "Исполнение и эффективность водителей");

    public static string SupplierSpend => T(
        en: "Supplier spend analysis",
        uk: "Аналіз витрат на постачальників",
        pl: "Analiza wydatków na dostawców",
        ru: "Анализ расходов на поставщиков");

    public static string Cancellations => T(
        en: "Cancellation & lost revenue",
        uk: "Скасування та втрачений дохід",
        pl: "Anulacje i utracony przychód",
        ru: "Отмены и потерянная выручка");

    private static string T(string en, string uk, string pl, string ru)
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
