using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Api.Authentication;

public static class AuthenticationRegistration
{
    /// <summary>
    /// Registra as opções do JWT validadas na subida (chave obrigatória, mínimo de 32 bytes).
    /// Usado também pelo Identity, que só emite tokens.
    /// </summary>
    public static IServiceCollection AddJwtOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options => Encoding.UTF8.GetByteCount(options.SigningKey) >= JwtOptions.MinimumKeyBytes,
                $"Defina a variável de ambiente 'Jwt__SigningKey' com pelo menos {JwtOptions.MinimumKeyBytes} caracteres.")
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Cada serviço valida o token sozinho (stateless, ADR-07): confere assinatura, emissor,
    /// audiência e validade, sem chamar o Identity a cada requisição.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddJwtOptions(configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configuração adiada (lida das opções só quando usada): o "dotnet ef" consegue
        // montar a aplicação mesmo sem a variável de ambiente definida.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization();
        return services;
    }
}
