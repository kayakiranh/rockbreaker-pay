var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "RockBreaker FakeGovernment SOAP service");
app.Run();
