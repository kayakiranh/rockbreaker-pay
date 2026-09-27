using CoreWCF;
using CoreWCF.Configuration;
using RockBreaker.FakeGovernment.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddServiceModelServices();

var app = builder.Build();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<GovernmentService>();
    serviceBuilder.AddServiceEndpoint<GovernmentService, IGovernmentService>(
        new BasicHttpBinding(),
        "/GovernmentService.svc");
});

app.MapGet("/", () => "RockBreaker FakeGovernment SOAP service");
app.Run();
