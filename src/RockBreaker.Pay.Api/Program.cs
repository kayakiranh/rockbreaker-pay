using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RockBreaker.Pay.Infrastructure.Auditing;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Infrastructure.Health;
using RockBreaker.Pay.Modules.Cutoff;
using RockBreaker.Pay.Modules.Compliance;
using RockBreaker.Pay.Modules.Campaigns;
using RockBreaker.Pay.Modules.Banking;
using RockBreaker.Pay.Modules.Fraud;
using RockBreaker.Pay.Modules.Government;
using RockBreaker.Pay.Modules.Identity.Abstractions;
using RockBreaker.Pay.Modules.Identity.Application;
using RockBreaker.Pay.Modules.Identity.Infrastructure;
using RockBreaker.Pay.Modules.Identity.Mapping;
using RockBreaker.Pay.Modules.Identity.Security;
using RockBreaker.Pay.Modules.MoneyRequests;
using RockBreaker.Pay.Modules.Notifications;
using RockBreaker.Pay.Modules.Reporting;
using RockBreaker.Pay.Modules.Instructions;
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
builder.Services.AddScoped<DatabaseBootstrapper>();
builder.Services.AddScoped<DevelopmentDataSeeder>();
builder.Services.AddScoped<IReadinessService, ReadinessService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IProfileMapper, ProfileMapper>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IKycGuard, KycGuard>();
builder.Services.AddScoped<IKycService, KycService>();

builder.Services.AddHttpClient<IKycProviderClient, KycProviderClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalServices:KycBaseUrl"] ?? "http://localhost:5106/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletTransferStore, WalletTransferStore>();
builder.Services.AddScoped<IWalletTransferService, WalletTransferService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IWalletLimitGuard, WalletLimitGuard>();
builder.Services.AddScoped<IMoneyRequestService, MoneyRequestService>();
builder.Services.AddScoped<IPaymentInstructionService, PaymentInstructionService>();
builder.Services.AddScoped<IPaymentInstructionProcessor, PaymentInstructionProcessor>();
builder.Services.AddHostedService<PaymentInstructionWorker>();
builder.Services.AddScoped<IOutboxNotificationProcessor, OutboxNotificationProcessor>();
builder.Services.AddHostedService<OutboxNotificationWorker>();
builder.Services.AddScoped<IFraudEvaluator, DatabaseFraudEvaluator>();
builder.Services.AddScoped<IFraudRuleService, FraudRuleService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IValidator<TransferRequest>, TransferRequestValidator>();
builder.Services.AddScoped<IAuditLogWriter, AuditLogWriter>();
builder.Services.AddScoped<IRegulatoryReportService, RegulatoryReportService>();
builder.Services.AddScoped<IGovernmentReportService, GovernmentReportService>();
builder.Services.AddScoped<IBillPaymentStore, BillPaymentStore>();
builder.Services.AddScoped<IBillPaymentService, BillPaymentService>();
builder.Services.AddScoped<IExternalWalletStore, ExternalWalletStore>();
builder.Services.AddScoped<IBankTransferService, BankTransferService>();

builder.Services.AddHttpClient<IElasticAuditWriter, ElasticAuditWriter>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Elasticsearch:BaseUrl"] ?? "http://localhost:9200/");
});

builder.Services.AddHttpClient("ElasticsearchHealth", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Elasticsearch:BaseUrl"] ?? "http://localhost:9200/");
    client.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddHttpClient<IGovernmentSoapClient, GovernmentSoapClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalServices:GovernmentBaseUrl"] ?? "http://localhost:5105/");
});

builder.Services.AddHttpClient<INotificationClient, NotificationClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalServices:NotificationBaseUrl"] ?? "http://localhost:5103/");
});

builder.Services.AddHttpClient<ICampaignClient, CampaignClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalServices:CampaignBaseUrl"] ?? "http://localhost:5102/");
});

builder.Services.AddHttpClient<IBankingClient, BankingClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalServices:BankingBaseUrl"] ?? "http://localhost:5101/");
});

builder.Services.AddHttpClient<ICutoffClient, CutoffClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalServices:CutoffBaseUrl"] ?? "http://localhost:5104/");
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("DatabaseBootstrap:Enabled"))
{
    using var bootstrapScope = app.Services.CreateScope();
    var bootstrapper = bootstrapScope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>();
    await bootstrapper.InitializeAsync();

    if (app.Configuration.GetValue<bool>("DatabaseBootstrap:SeedDemoData"))
    {
        var seeder = bootstrapScope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
        await seeder.SeedAsync();
    }
}

app.UseMiddleware<RequestAuditMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseOpenApi(settings =>
{
    settings.Path = "/openapi/{documentName}.json";
});

app.MapControllers();

app.Run();
