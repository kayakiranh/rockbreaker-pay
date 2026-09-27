using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RockBreaker.Pay.Infrastructure.Auditing;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Cutoff;
using RockBreaker.Pay.Modules.Fraud;
using RockBreaker.Pay.Modules.Identity.Abstractions;
using RockBreaker.Pay.Modules.Identity.Application;
using RockBreaker.Pay.Modules.Identity.Infrastructure;
using RockBreaker.Pay.Modules.Identity.Mapping;
using RockBreaker.Pay.Modules.Identity.Security;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;
using RockBreaker.Pay.Modules.Wallet.Infrastructure;
using RockBreaker.Pay.Modules.Wallet.Validation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) =>
    configuration.Enrich.FromLogContext().WriteTo.Console());

builder.Services.AddControllers();

builder.Services.AddOpenApiDocument(settings =>
{
    settings.DocumentName = "v1";
    settings.Title = "RockBreaker Pay API";
    settings.Version = "v1";
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IProfileMapper, ProfileMapper>();
builder.Services.AddScoped<IIdentityService, IdentityService>();

builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletTransferStore, WalletTransferStore>();
builder.Services.AddScoped<IWalletTransferService, WalletTransferService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IFraudEvaluator, DatabaseFraudEvaluator>();
builder.Services.AddScoped<IValidator<TransferRequest>, TransferRequestValidator>();
builder.Services.AddScoped<IAuditLogWriter, AuditLogWriter>();

builder.Services.AddHttpClient<IElasticAuditWriter, ElasticAuditWriter>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Elasticsearch:BaseUrl"] ?? "http://localhost:9200/");
});

builder.Services.AddHttpClient<ICutoffClient, CutoffClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalServices:CutoffBaseUrl"] ?? "http://localhost:5104/");
});

var app = builder.Build();

app.UseMiddleware<RequestAuditMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseOpenApi(settings =>
{
    settings.Path = "/openapi/{documentName}.json";
});

app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

app.Run();
