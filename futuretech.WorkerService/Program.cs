using futuretech.WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging", configureConnectionFactory: (connectionFactory) =>
{
    connectionFactory.ClientProvidedName = "app:event-consumer";
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
