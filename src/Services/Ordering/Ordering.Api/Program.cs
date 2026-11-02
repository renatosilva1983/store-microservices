using BuildingBlocks.Api.Authentication;
using BuildingBlocks.Api.Observability;
using Ordering.Api.ErrorHandling;
using Ordering.Application;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Composition Root do serviço de Pedidos — mesma estrutura do Catálogo.
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// /health também verifica o banco (e o MassTransit registra o estado do barramento).
builder.Services.AddHealthChecks().AddDbContextCheck<OrderingDbContext>("ordering-db");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.MigrateOrderingDatabaseAsync();
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
