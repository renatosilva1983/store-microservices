using BuildingBlocks.Application.Security;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Api.Authentication;

/// <summary>
/// Adaptador de <see cref="ICurrentUser"/>: lê as claims do token da requisição atual.
/// Os nomes das claims são os do JWT (sub, email, name, role), porque o MapInboundClaims
/// está desligado em <see cref="AuthenticationRegistration"/>.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? Id => Claim("sub");

    public string? Email => Claim("email");

    public string? Name => accessor.HttpContext?.User.Identity?.Name;

    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;

    private string? Claim(string type) => accessor.HttpContext?.User.FindFirst(type)?.Value;
}
