// API Gateway (ADR-04): único ponto de entrada do front.
// O YARP só encaminha: rotas e destinos vêm do appsettings (seção "ReverseProxy").
// O Gateway não referencia nenhum serviço — não conhece Domain, Application nem banco.
// Também é onde ficam as proteções de borda (rate limiting e headers, ADR-41).

using Store.Gateway.Security;

const string FrontendCorsPolicy = "frontend";

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// CORS num lugar só: os serviços internos não precisam liberar o navegador.
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id")
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithExposedHeaders("Location", "Retry-After")));

builder.Services.AddGatewaySecurity(builder.Configuration);

// O header "Server: Kestrel" é escrito pelo próprio servidor, depois do pipeline:
// só some desligando na configuração do Kestrel.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

builder.Services.AddHealthChecks();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseGatewaySecurity();
app.UseCors();
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok(new { service = "Store.Gateway", status = "ok" }));
app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();
