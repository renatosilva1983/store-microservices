namespace BuildingBlocks.Application.Security;

/// <summary>
/// Quem está fazendo a requisição, lido do token JWT já validado.
/// Porta da Application (Ports and Adapters): os casos de uso perguntam "quem é o usuário?"
/// sem conhecer HTTP; a implementação fica na borda (BuildingBlocks.Api).
/// </summary>
public interface ICurrentUser
{
    /// <summary>Identificador do usuário (claim <c>sub</c>). Nulo se não autenticado.</summary>
    string? Id { get; }

    string? Email { get; }

    string? Name { get; }

    bool IsInRole(string role);
}

public static class Roles
{
    public const string Admin = "admin";
    public const string Customer = "customer";
}
