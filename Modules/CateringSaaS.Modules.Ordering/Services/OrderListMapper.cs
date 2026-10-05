using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Modules.Ordering.DTOs;
using CateringSaaS.Shared.Contracts;

namespace CateringSaaS.Modules.Ordering.Services;

public sealed class OrderListMapper
{
    private readonly IClientCompanyLookup _clients;

    public OrderListMapper(IClientCompanyLookup clients)
    {
        _clients = clients;
    }

    public async Task<OrderListItemResponse> MapAsync(Order order, CancellationToken cancellationToken)
    {
        var mapped = await MapAsync(order.WorkspaceId, [order], cancellationToken);
        return mapped[0];
    }

    public async Task<IReadOnlyList<OrderListItemResponse>> MapAsync(
        Guid workspaceId,
        IReadOnlyList<Order> orders,
        CancellationToken cancellationToken)
    {
        var contacts = await _clients.GetContactsAsync(
            workspaceId,
            orders.Select(o => o.ClientCompanyId),
            cancellationToken);

        return orders
            .Select(order =>
            {
                contacts.TryGetValue(order.ClientCompanyId, out var client);
                return OrderDtoMapper.ToListItem(order, client);
            })
            .ToList();
    }
}
