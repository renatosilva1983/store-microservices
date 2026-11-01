using BuildingBlocks.Api.Authentication;
using BuildingBlocks.Api.Observability;
using Catalog.Api.ErrorHandling;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Composition Root: cada camada registra o que é dela; a Api só monta as peças.
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// /health também verifica o banco (e o MassTransit registra o estado do barramento).
builder.Services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>("catalog-db");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.MigrateCatalogDatabaseAsync();
}

app.UseServiceDefaults();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.MapServiceDefaults();

app.Run();

// Permite testes de integração com WebApplicationFactory<Program>.
public partial class Program;
