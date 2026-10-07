namespace CateringSaaS.Modules.Reporting.Services;

/// <summary>
/// Localized metric / column / series labels for operational reports.
/// Resolves from CurrentUICulture (Accept-Language or assistant message culture).
/// </summary>
public static class ReportLabels
{
    public static string Revenue => L("Revenue", "Дохід", "Przychód", "Выручка");
    public static string Portions => L("Portions", "Порції", "Porcje", "Порции");
    public static string Orders => L("Orders", "Замовлення", "Zamówienia", "Заказы");
    public static string Clients => L("Clients", "Клієнти", "Klienci", "Клиенты");
    public static string Client => L("Client", "Клієнт", "Klient", "Клиент");
    public static string Driver => L("Driver", "Водій", "Kierowca", "Водитель");
    public static string Ingredient => L("Ingredient", "Інгредієнт", "Składnik", "Ингредиент");
    public static string Unit => L("Unit", "Од.", "Jedn.", "Ед.");
    public static string Status => L("Status", "Статус", "Status", "Статус");
    public static string Date => L("Date", "Дата", "Data", "Дата");
    public static string Day => L("Day", "День", "Dzień", "День");
    public static string Dish => L("Dish", "Страва", "Danie", "Блюдо");
    public static string Supplier => L("Supplier", "Постачальник", "Dostawca", "Поставщик");
    public static string Reason => L("Reason", "Причина", "Powód", "Причина");
    public static string Reviews => L("Reviews", "Відгуки", "Opinie", "Отзывы");
    public static string Rating => L("Rating", "Оцінка", "Ocena", "Оценка");
    public static string Comments => L("Comments", "Коментарі", "Komentarze", "Комментарии");
    public static string Dishes => L("Dishes", "Страви", "Dania", "Блюда");
    public static string Unassigned => L("Unassigned", "Не призначено", "Nieprzypisany", "Не назначен");
    public static string Document => L("Document", "Документ", "Dokument", "Документ");

    public static string OrdersToday => L("Orders today", "Замовлення сьогодні", "Zamówienia dziś", "Заказы сегодня");
    public static string RevenueToday => L("Revenue today", "Дохід сьогодні", "Przychód dziś", "Выручка сегодня");
    public static string PortionsToday => L("Portions today", "Порції сьогодні", "Porcje dziś", "Порции сегодня");
    public static string AssignedForDelivery => L("Assigned for delivery", "Призначено на доставку", "Przypisane do dostawy", "Назначено на доставку");
    public static string UnassignedReadyOrders => L("Unassigned ready orders", "Готові без водія", "Gotowe bez kierowcy", "Готовые без водителя");
    public static string CriticalStockItems => L("Critical stock items", "Критичні залишки", "Krytyczne stany", "Критические остатки");
    public static string ReadyPortions => L("Ready portions", "Готові порції", "Gotowe porcje", "Готовые порции");
    public static string InProductionPortions => L("In production portions", "Порції у виробництві", "Porcje w produkcji", "Порции в производстве");
    public static string WaitingPortions => L("Waiting portions", "Порції в очікуванні", "Porcje oczekujące", "Порции в ожидании");
    public static string KitchenReadiness => L("Kitchen readiness", "Готовність кухні", "Gotowość kuchni", "Готовность кухни");
    public static string TodaysOrders => L("Today's orders", "Замовлення на сьогодні", "Dzisiejsze zamówienia", "Заказы на сегодня");
    public static string CriticalStock => L("Critical stock", "Критичні залишки", "Krytyczne stany", "Критические остатки");
    public static string OnHand => L("On hand", "В наявності", "Na stanie", "В наличии");
    public static string Min => L("Min", "Мін.", "Min.", "Мин.");
    public static string OrdersByStatus => L("Orders by status", "Замовлення за статусом", "Zamówienia wg statusu", "Заказы по статусу");
    public static string RevenueByClient => L("Revenue by client", "Виручка за клієнтами", "Przychód wg klienta", "Выручка по клиентам");
    public static string NPortions(int n) => L($"{n} portions", $"{n} порцій", $"{n} porcji", $"{n} порций");

    public static string Purchased => L("Purchased", "Закуплено", "Zakupiono", "Закуплено");
    public static string Used => L("Used", "Витрачено", "Zużyto", "Израсходовано");
    public static string Consumed => L("Consumed", "Списано", "Zużyto", "Списано");
    public static string UsedShare => L("Used share", "Частка використання", "Udział zużycia", "Доля использования");
    public static string Adjustments => L("Adjustments", "Коригування", "Korekty", "Корректировки");
    public static string SpendByCategory => L("Spend by category", "Витрати за категоріями", "Wydatki wg kategorii", "Расходы по категориям");
    public static string DailyStockMovements => L("Daily stock movements", "Рух складу по днях", "Ruch magazynu dziennie", "Движение склада по дням");
    public static string PurchasedQty => L("Purchased qty", "Закуплено (к-сть)", "Zakupiono (ilość)", "Закуплено (кол-во)");
    public static string PurchasedCost => L("Purchased cost", "Закуплено (сума)", "Zakupiono (koszt)", "Закуплено (сумма)");
    public static string UsedQty => L("Used qty", "Витрачено (к-сть)", "Zużyto (ilość)", "Израсходовано (кол-во)");
    public static string UsedCost => L("Used cost", "Витрачено (сума)", "Zużyto (koszt)", "Израсходовано (сумма)");

    public static string DeliveredOrders => L("Delivered orders", "Доставлені замовлення", "Dostarczone zamówienia", "Доставленные заказы");
    public static string Reclamations => L("Reclamations", "Рекламації", "Reklamacje", "Рекламации");
    public static string AverageRating => L("Average rating", "Середня оцінка", "Średnia ocena", "Средняя оценка");
    public static string DeliveredOrdersReviews => L("Delivered orders & reviews", "Доставки та відгуки", "Dostawy i opinie", "Доставки и отзывы");
    public static string ReviewMix => L("Review mix", "Структура відгуків", "Struktura opinii", "Структура отзывов");
    public static string OtherReviews => L("Other reviews", "Інші відгуки", "Inne opinie", "Прочие отзывы");

    public static string PortionsCooked => L("Portions cooked", "Зварені порції", "Ugotowane porcje", "Приготовленные порции");
    public static string DishRevenue => L("Dish revenue", "Дохід зі страв", "Przychód z dań", "Выручка по блюдам");
    public static string TopDishes => L("Top dishes", "Топ страв", "Top dań", "Топ блюд");
    public static string PortionsByDish => L("Portions by dish", "Порції за стравами", "Porcje wg dań", "Порции по блюдам");

    public static string ReclamationsByClientDish => L("Reclamations by client and dish", "Рекламації за клієнтом і стравою", "Reklamacje wg klienta i dania", "Рекламации по клиенту и блюду");
    public static string AvgRating => L("Avg rating", "Сер. оцінка", "Śr. ocena", "Ср. оценка");
    public static string Worst => L("Worst", "Найгірша", "Najgorsza", "Худшая");
    public static string ReclamationsByDish => L("Reclamations by dish", "Рекламації за стравами", "Reklamacje wg dań", "Рекламации по блюдам");

    public static string Ingredients => L("Ingredients", "Інгредієнти", "Składniki", "Ингредиенты");
    public static string ExpectedUsage => L("Expected usage", "Очікувана витрата", "Oczekiwane zużycie", "Ожидаемый расход");
    public static string ActualConsumption => L("Actual consumption", "Фактична витрата", "Rzeczywiste zużycie", "Фактический расход");
    public static string OverConsumed => L("Over-consumed", "Понад норму", "Powyżej normy", "Сверх нормы");
    public static string Expected => L("Expected", "Очікувано", "Oczekiwane", "Ожидаемо");
    public static string Actual => L("Actual", "Факт", "Faktyczne", "Факт");
    public static string ExpectedVsActual => L("Expected vs actual consumption", "Очікувана vs фактична витрата", "Oczekiwane vs faktyczne zużycie", "Ожидаемый vs фактический расход");
    public static string Variance => L("Variance", "Відхилення", "Odchylenie", "Отклонение");
    public static string VariancePercent => L("Variance %", "Відхилення %", "Odchylenie %", "Отклонение %");

    public static string OrderRevenue => L("Order revenue", "Дохід із замовлень", "Przychód z zamówień", "Выручка с заказов");
    public static string FoodCostPercent => L("Food-cost %", "Food-cost %", "Food-cost %", "Food-cost %");
    public static string AdjustmentsSpoilage => L("Adjustments / spoilage", "Коригування / псування", "Korekty / straty", "Корректировки / порча");
    public static string UsedShareOfPurchases => L("Used share of purchases", "Частка закупівель використана", "Udział zużytych zakupów", "Доля использованных закупок");
    public static string CostMix => L("Cost mix", "Структура витрат", "Struktura kosztów", "Структура затрат");
    public static string RemainingPurchases => L("Remaining purchases", "Залишок закупівель", "Pozostałe zakupy", "Остаток закупок");
    public static string DailyUsageCost => L("Daily usage cost", "Вартість використання по днях", "Koszt zużycia dziennie", "Стоимость расхода по дням");
    public static string ConsumedCostHint => L("consumed / revenue", "списано / дохід", "zużyto / przychód", "списано / выручка");

    public static string Shortages => L("Shortages", "Дефіцит", "Braki", "Дефицит");
    public static string ConfirmedPortions => L("Confirmed portions", "Підтверджені порції", "Potwierdzone porcje", "Подтверждённые порции");
    public static string ToBuy => L("To buy", "Закупити", "Do kupienia", "Закупить");
    public static string Required => L("Required", "Потрібно", "Wymagane", "Нужно");
    public static string ShoppingShortageForecast => L("Shopping & shortage forecast", "Прогноз закупівель і дефіциту", "Prognoza zakupów i braków", "Прогноз закупок и дефицита");

    public static string Drivers => L("Drivers", "Водії", "Kierowcy", "Водители");
    public static string DeliveredRevenue => L("Delivered revenue", "Виручка з доставок", "Przychód z dostaw", "Выручка с доставок");
    public static string DeliveredOrdersByDriver => L("Delivered orders by driver", "Доставки за водіями", "Dostawy wg kierowców", "Доставки по водителям");
    public static string DriverFulfillment => L("Driver fulfillment", "Виконання водіями", "Realizacja kierowców", "Исполнение водителями");

    public static string PurchaseSpend => L("Purchase spend", "Витрати на закупівлі", "Wydatki na zakupy", "Расходы на закупки");
    public static string VolumeBought => L("Volume bought", "Обсяг закупівель", "Wolumen zakupów", "Объём закупок");
    public static string Suppliers => L("Suppliers", "Постачальники", "Dostawcy", "Поставщики");
    public static string Receipts => L("Receipts", "Надходження", "Przyjęcia", "Поступления");
    public static string SpendBySupplier => L("Spend by supplier", "Витрати за постачальниками", "Wydatki wg dostawców", "Расходы по поставщикам");
    public static string SupplierSpend => L("Supplier spend", "Витрати на постачальників", "Wydatki na dostawców", "Расходы на поставщиков");
    public static string Spend => L("Spend", "Витрати", "Wydatki", "Расходы");
    public static string Volume => L("Volume", "Обсяг", "Wolumen", "Объём");
    public static string SharePercent => L("Share %", "Частка %", "Udział %", "Доля %");

    public static string CancelledOrders => L("Cancelled orders", "Скасовані замовлення", "Anulowane zamówienia", "Отменённые заказы");
    public static string LostRevenue => L("Lost revenue", "Втрачений дохід", "Utracony przychód", "Потерянная выручка");
    public static string LostPortions => L("Lost portions", "Втрачені порції", "Utracone porcje", "Потерянные порции");
    public static string CancellationsByClientDate => L("Cancellations by client and date", "Скасування за клієнтом і датою", "Anulacje wg klienta i daty", "Отмены по клиенту и дате");
    public static string LostRevenueByClient => L("Lost revenue by client", "Втрачений дохід за клієнтами", "Utracony przychód wg klienta", "Потерянная выручка по клиентам");

    public static string PendingConfirmation(int n) =>
        L($"{n} pending confirmation", $"{n} очікують підтвердження", $"{n} czeka na potwierdzenie", $"{n} ждут подтверждения");

    public static string ActiveOrders(int n) =>
        L($"{n} active orders", $"{n} активних замовлень", $"{n} aktywnych zamówień", $"{n} активных заказов");

    public static string ReadyCount(int n) =>
        L($"{n} ready", $"{n} готово", $"{n} gotowe", $"{n} готово");

    public static string AcrossClients(int n) =>
        L($"across {n} clients", $"серед {n} клієнтів", $"wśród {n} klientów", $"среди {n} клиентов");

    private static string L(string en, string uk, string pl, string ru) => ReportL10n.T(en, uk, pl, ru);
}
