using BuildingBlocks.Api.Authentication;
using BuildingBlocks.Api.Observability;
using Identity.Api.Auth;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Serviço de identidade: só emite tokens. Quem valida são as próprias APIs (ADR-07).
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddJwtOptions(builder.Configuration);
builder.Services.Configure<DemoUsersOptions>(builder.Configuration.GetSection(DemoUsersOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();

var app = builder.Build();

app.UseServiceDefaults();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.MapServiceDefaults();

app.Run();
