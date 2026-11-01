// API Gateway (ADR-04): único ponto de entrada do front.
// O YARP só encaminha: rotas e destinos vêm do appsettings (seção "ReverseProxy").
// O Gateway não referencia nenhum serviço — não conhece Domain, Application nem banco.

const string FrontendCorsPolicy = "frontend";

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// CORS num lugar só: os serviços internos não precisam liberar o navegador.
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Location")));

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new { service = "Store.Gateway", status = "ok" }));
app.MapReverseProxy();

app.Run();
