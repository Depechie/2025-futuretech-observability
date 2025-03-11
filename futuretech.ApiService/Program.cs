using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using futuretech.ApiService;
using futuretech.ApiService.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

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
