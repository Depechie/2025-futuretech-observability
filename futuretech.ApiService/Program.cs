using futuretech.ApiService;
using futuretech.ApiService.Extensions;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("Todos");

// Add REDIS distributed cache.
builder.AddRedisDistributedCache("cache");

// Add RabbitMQ client.
builder.AddRabbitMQClient("messaging", configureConnectionFactory: (connectionFactory) =>
{
    connectionFactory.ClientProvidedName = "app:event-producer";
});

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var connectionString = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>().ConnectionString;
    DatabaseInitializer.Initialize(connectionString, "user", "password");
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapEndpoints();
app.MapDefaultEndpoints();

app.Run();
