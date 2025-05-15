var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .PublishAsContainer();

var apiService = builder.AddProject<Projects.futuretech_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging);   

builder.AddProject<Projects.futuretech_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.futuretech_WorkerService>("serviceworker")
    .WithReference(messaging)
    .WaitFor(messaging);

builder.Build().Run();
