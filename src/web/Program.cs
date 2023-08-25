var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // Necessary for actually starting to route some API endpoints

var app = builder.Build();

app.MapControllers(); // Necessary for actually starting to route some API endpoints

app.Run();