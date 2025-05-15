var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var apiService = builder.AddProject<Projects.futuretech_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.futuretech_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
