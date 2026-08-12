using CateringSaaS.Modules.Kitchen.DTOs;
using CateringSaaS.Shared.Contracts;

namespace CateringSaaS.Modules.Kitchen.Services;

public sealed class KitchenServiceException : Exception
{
    public int StatusCode { get; }

    public IReadOnlyList<StockShortageResponse>? Shortages { get; }

    public KitchenServiceException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }

    public KitchenServiceException(
        string message,
        IReadOnlyList<StockShortageResponse> shortages,
        int statusCode = 409) : base(message)
    {
        StatusCode = statusCode;
        Shortages = shortages;
    }
}

internal sealed record ProductionRequirements(
    IReadOnlyList<DishToCookResponse> DishesToCook,
    IReadOnlyDictionary<Guid, decimal> IngredientTotals);
