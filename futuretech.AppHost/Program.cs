var builder = DistributedApplication.CreateBuilder(args);

var todosDbName = "Todos";
var username = builder.AddParameter("username", "user", secret: true);
var password = builder.AddParameter("password", "password", secret: true);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .WithPasswordAuthentication(username, password)
    .RunAsContainer();

var todosDb = postgres.AddDatabase(todosDbName);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .PublishAsContainer();

var apiService = builder.AddProject<Projects.futuretech_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(todosDb)
    .WaitFor(todosDb);

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
