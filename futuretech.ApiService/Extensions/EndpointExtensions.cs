using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace futuretech.ApiService.Extensions;

public static class EndpointExtensions
{
    private static readonly ActivitySource _activitySource = new("Aspire.RabbitMQ.Client");
    private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;    

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/weatherforecast", GetWeatherforecast).WithName("GetWeatherForecast");

        return app;
    }

    private static async Task<IEnumerable<WeatherForecast>> GetWeatherforecast(IDistributedCache cache, IConnection messageConnection)
    {
        // https://www.rabbitmq.com/client-libraries/dotnet-api-guide#connection-and-channel-lifespan
        using var messageChannel = await messageConnection.CreateChannelAsync();
        await messageChannel.QueueDeclareAsync("queue", durable: true, exclusive: false, autoDelete: false);

        using var activity = _activitySource.StartActivity($"queue publish", ActivityKind.Producer);
        var properties = new BasicProperties();
        properties.Persistent = true;
        AddActivityToHeader(activity, properties);

        var cachedForecast = await cache.GetAsync("forecast");
        IEnumerable<WeatherForecast> forecast = null;

        if (cachedForecast is null)
        {
            var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
            forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            )).ToArray();

            await cache.SetAsync("forecast", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(forecast)), new ()
            {
                AbsoluteExpiration = DateTime.Now.AddSeconds(15)
            });
        }
        else
            forecast = JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(cachedForecast);        

        await messageChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "queue",
            mandatory: true,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(forecast)));

        return forecast;
    }

    private static void AddActivityToHeader(Activity activity, IBasicProperties props)
    {
        try
        {
            _propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props, InjectContextIntoHeader);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
            activity?.SetTag("messaging.destination", string.Empty);
            activity?.SetTag("messaging.rabbitmq.routing_key", "queue");
        }
        catch(Exception ex)
        {
            var t = ex.Message;
        }
    }

    private static void InjectContextIntoHeader(IBasicProperties props, string key, string value)
    {
        try
        {
            props.Headers ??= new Dictionary<string, object>();
            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Failed to inject trace context");
        }
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
