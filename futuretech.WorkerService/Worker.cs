using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace futuretech.WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private static readonly ActivitySource _activitySource = new("Aspire.RabbitMQ.Client");
    private static readonly TextMapPropagator _propagator = new TraceContextPropagator();
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _messageConnection;
    private IChannel? _messageChannel;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
             _logger.LogInformation($"Awaiting messages...");

            string queueName = "queue";

            _messageConnection = _serviceProvider.GetRequiredService<IConnection>();
            _messageChannel = await _messageConnection.CreateChannelAsync();
            await _messageChannel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(_messageChannel);
            consumer.ReceivedAsync += async (s, e) => await ProcessMessageAsync(s, e);

            await _messageChannel.BasicConsumeAsync(queue: queueName,
                                         autoAck: true,
                                         consumer: consumer);
        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        _messageChannel?.Dispose();
        _messageConnection?.Dispose();
    }

    private async Task ProcessMessageAsync(object? sender, BasicDeliverEventArgs args)
    {
        var parentContext = _propagator.Extract(default, args.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        using var activity = _activitySource.StartActivity($"queue receive", ActivityKind.Consumer, parentContext.ActivityContext);
        if (activity is not null)
        {
            AddActivityTags(activity);
            _logger.LogInformation($"Processing message at: {DateTime.UtcNow}");

            var jsonSpecified = Encoding.UTF8.GetString(args.Body.Span);            
            _logger.LogInformation($"Message received: {jsonSpecified}");
            // var item = JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(jsonSpecified);        
            // if (item is not null)
            // {                
            // }
        }
    }

    private IEnumerable<string> ExtractTraceContextFromBasicProperties(IReadOnlyBasicProperties props, string key)
    {
        try
        {
            if (props.Headers != null && 
                props.Headers.TryGetValue(key, out var value) && 
                value is byte[] bytes)
            {
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to extract trace context: {ex}");
        }

        return Enumerable.Empty<string>();
    }

    private void AddActivityTags(Activity activity)
    {
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination_kind", "queue");
        activity?.SetTag("messaging.destination", string.Empty);
        activity?.SetTag("messaging.rabbitmq.routing_key", "queue");
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}