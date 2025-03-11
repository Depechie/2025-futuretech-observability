var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.futuretech_ApiService>("apiservice");

builder.AddProject<Projects.futuretech_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
