var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapControllers();
app.MapOpenApi("/openapi/{documentName}.json");
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

app.Run();
