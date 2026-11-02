namespace Identity.Api.Auth;

/// <summary>
/// Usuários de demonstração (simplificação consciente, ADR-07).
/// Em produção: Keycloak, Entra ID ou ASP.NET Identity com senha em hash.
/// </summary>
public sealed class DemoUsersOptions
{
    public const string SectionName = "Identity";

    public List<DemoUser> Users { get; init; } = [];
}

public sealed class DemoUser
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Role { get; init; } = "customer";
}
