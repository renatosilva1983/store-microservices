namespace BuildingBlocks.Api.Authentication;

/// <summary>
/// Configuração do JWT, igual em quem emite (Identity) e em quem valida (Catalog, Ordering).
/// A chave de assinatura NÃO fica no appsettings: vem da variável de ambiente Jwt__SigningKey.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; init; } = "store-identity";

    public string Audience { get; init; } = "store-api";

    public string SigningKey { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 120;
}
