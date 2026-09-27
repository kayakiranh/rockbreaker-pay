using FluentValidation;
using RockBreaker.Pay.Infrastructure.Auditing;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Cutoff;
using RockBreaker.Pay.Modules.Fraud;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;
using RockBreaker.Pay.Modules.Wallet.Infrastructure;
using RockBreaker.Pay.Modules.Wallet.Validation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletTransferStore, WalletTransferStore>();
builder.Services.AddScoped<IWalletTransferService, WalletTransferService>();
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
app.MapControllers();
app.MapOpenApi("/openapi/{documentName}.json");
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

app.Run();
