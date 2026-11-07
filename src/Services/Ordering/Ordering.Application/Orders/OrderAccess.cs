using BuildingBlocks.Application.Security;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders;

/// <summary>
/// Regra de acesso aos pedidos, num lugar só (proteção contra BOLA/IDOR, OWASP API1):
/// estar logado não basta, o pedido precisa ser do usuário. O administrador vê todos.
/// O dono vem sempre do token (<see cref="ICurrentUser"/>), nunca da requisição.
/// </summary>
public static class OrderAccess
{
    /// <summary>
    /// Filtro de dono para as consultas: o id do usuário, ou nulo para o administrador (sem filtro).
    /// </summary>
    public static string? OwnerFilter(ICurrentUser user) =>
        user.IsInRole(Roles.Admin) ? null : user.Id ?? string.Empty;

    public static bool CanAccess(Order order, ICurrentUser user) =>
        user.IsInRole(Roles.Admin) || (user.Id is not null && order.CustomerId == user.Id);
}
