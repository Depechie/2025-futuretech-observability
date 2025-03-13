# 2025-futuretech-observability

# Workshop

## Disclaimer, copyright and code

*This workshop is licensed under CC BY-NC-SA 4.0 and should not be used commercially without permission.*  

*Of course, the concepts we discuss can and should be used to improve the quality of your application production environment, as such all code samples are licensed under MIT.*

## Init Aspire project

```
dotnet new install Aspire.ProjectTemplates
dotnet new aspire-starter --name futuretech
dotnet new aspire-starter --output futuretech
```

The `--name` parameter specifies the name of the project, and the `--output` parameter specifies the output subdirectory.

### Run the project

In the terminal type

```
dotnet run --project futuretech.AppHost
```

Press `ctrl-c` or `cmd-c` to stop the application

> [!NOTE]
> Go over the project!
> Explain the AppHost project and how the orchestration works with the given C# code
> Explain the OpenTelemetry integration through the service defaults project
> Explain other aspects of the service defaults project
> Explain service discovery and how it is tied to environment variables
> Explain other environment variables

## Add integrations

It is possible to add integrations to the project that are provided by .NET Aspire team.
An integration is a NuGet package that contains a set of features that can be added to the AppHost project and used/added in a client project.

Community driven integrations are also available. You can find them in the [Aspire Community GitHub repository](https://github.com/CommunityToolkit/Aspire).

### Add Redis cache integration

[Redis Output Cache](https://learn.microsoft.com/en-us/dotnet/aspire/caching/stackexchange-redis-output-caching-integration?tabs=dotnet-cli&pivots=redis)
[Redis Output Cache example](https://learn.microsoft.com/en-us/dotnet/aspire/caching/caching-integrations?tabs=dotnet-cli)

#### Host project

Go to the AppHost project directory and run the following command:

```
dotnet add package Aspire.Hosting.Redis
```

> [!NOTE]
> Explain the addition of the Redis cache integration in Program.cs of the AppHost project
> Explain the WithReference extension method

```
var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var apiService = builder.AddProject<Projects.PortoTechhub_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.PortoTechhub_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache);
```

#### Web project

In the Web project directory run the following command

```

dotnet add package Aspire.StackExchange.Redis.OutputCaching

```

> [!NOTE]
> Explain the addition of the Redis cache integration in Program.cs of the Web project

```
// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add REDIS output cache.
builder.AddRedisOutputCache("cache");
```

> [!NOTE]
> Explain that we will disable client side output caching in Weather.razor of the Web project ( we will be using the caching on the API level )

```
@page "/weather"
@attribute [StreamRendering(true)]
@* @attribute [OutputCache(Duration = 5)] *@
```

#### API project

In the API project directory run the following command

```
dotnet add package Aspire.StackExchange.Redis.DistributedCaching
```

> [!NOTE]
> Explain the addition of the Redis cache integration in Program.cs of the API project

```
// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add REDIS distributed cache.
builder.AddRedisDistributedCache("cache");
```

```
app.MapGet("/weatherforecast", async (IDistributedCache cache) =>
{
    var cachedForecast = await cache.GetAsync("forecast");

    if (cachedForecast is null)
    {
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

        await cache.SetAsync("forecast", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(forecast)), new ()
        {
            AbsoluteExpiration = DateTime.Now.AddSeconds(15)
        });

        return forecast;
    }

    return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(cachedForecast);
})
.WithName("GetWeatherForecast");
```

While the Aspire project is running, you can look at the Redis cache through RedisInsight and see the keys.

Look for the `forecast` key in the Redis cache.

### Add RabbitMQ integration

https://www.cloudamqp.com/blog/part3-rabbitmq-for-beginners_the-management-interface.html

#### Host project

Go to the AppHost project directory and run the following command:

```
dotnet add package Aspire.Hosting.RabbitMQ
```

```
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .PublishAsContainer();

var apiService = builder.AddProject<Projects.futuretech_WorkerServices>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging);

var workerService = builder.AddProject<Projects.futuretech_WorkerService>("workerservice")
    .WithReference(messaging)
    .WaitFor(messaging);
```

#### API project

In the API project directory run the following command:

```
dotnet add package Aspire.RabbitMQ.Client.v7
```

In the Program.cs add the following:

```
// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add REDIS distributed cache.
builder.AddRedisDistributedCache("cache");

// Add RabbitMQ client.
builder.AddRabbitMQClient("messaging", configureConnectionFactory: (connectionFactory) =>
{
    connectionFactory.ClientProvidedName = "app:event-producer";
});
```

Extract endpoint mapping to extension method
Send message to message queue
Tag message with activity information in header

#### Worker service project

Create new worker service project
Add it as project reference to the AppHost project, that way the Projects enumerator will contain futuretech_WorkerService
Add a project reference to the ServiceDefaults into the WorkerService
Implement the worker that picks up the message

### Add PostgreSQL integration

https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-postgresql-integration?tabs=dotnet-cli
Flexible Server is a relational database service based on the open-source Postgres database engine. It's a fully managed database-as-a-service that can handle mission-critical workloads with predictable performance, security, high availability, and dynamic scalability.

#### Host project

Go to the AppHost project directory and run the following command:

```
dotnet add package Aspire.Hosting.Azure.PostgreSQL
```

In the Program.cs add the following:

```
var todosDbName = "Todos";
var username = builder.AddParameter("username", "user", secret: true);
var password = builder.AddParameter("password", "password", secret: true);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .WithPasswordAuthentication(username, password)
    .RunAsContainer();

var todosDb = postgres.AddDatabase(todosDbName);

var apiService = builder.AddProject<Projects.PortoTechhub_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(todosDb)
    .WaitFor(todosDb);
```

#### Web project

In the Pages folder add a new `Todo.razor` page

```
@page "/todo"
@attribute [StreamRendering(true)]

@inject TodoApiClient TodoApi

<PageTitle>Todo</PageTitle>

<h1>Todo</h1>

<p>This component demonstrates showing data loaded from a backend API service.</p>

@if (todos == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Id</th>
                <th>Title</th>
                <th>Is Completed</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var todo in todos)
            {
                <tr>
                    <td>@todo.Id</td>
                    <td>@todo.Title</td>
                    <td>@todo.IsCompleted</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private TodoItem[]? todos;

    protected override async Task OnInitializedAsync()
    {
        todos = await TodoApi.GetAllTodosAsync();
    }
}
```

Edit the NavMenu.razor page

```
<div class="nav-item px-3">
	<NavLink class="nav-link" href="weather">
		<span class="bi bi-list-nested" aria-hidden="true"></span> Weather
	</NavLink>
</div>

<div class="nav-item px-3">
	<NavLink class="nav-link" href="todo">
		<span class="bi bi-list-nested" aria-hidden="true"></span> Todo
	</NavLink>
</div>
```

In the Program.cs file init a new client

```
builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });

builder.Services.AddHttpClient<TodoApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });
```

Also add a `TodoApiClient.cs` file

```
namespace futuretech.Web;

public class TodoApiClient(HttpClient httpClient)
{
    public async Task<TodoItem[]> GetAllTodosAsync(CancellationToken cancellationToken = default)
    {
        List<TodoItem>? todos = null;

        await foreach (var todo in httpClient.GetFromJsonAsAsyncEnumerable<TodoItem>("/todos", cancellationToken))
        {
            if (todo is not null)
            {
                todos ??= [];
                todos.Add(todo);
            }
        }

        return todos?.ToArray() ?? [];
    }

    public async Task<TodoItem?> GetTodoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<TodoItem>($"/todos/{id}", cancellationToken);
    }
}

public record TodoItem(int Id, string Title, bool IsCompleted);
```

#### API project

Go to the API project directory and run the following command:

```
dotnet add package Aspire.Npgsql
dotnet add package Dapper
```

In the Program.cs add:

```
using Npgsql;

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("Todos");
```

```
var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var connectionString = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>().ConnectionString;
    DatabaseInitializer.Initialize(connectionString, "user", "password");
}
```

Add the `DatabaseInitializer.cs` file

```
using Npgsql;

namespace futuretech.ApiService;

public static class DatabaseInitializer
{
    public static void Initialize(string connectionString, string username, string password)
    {
        EnsureDatabaseExists(connectionString, username, password);
        EnsureTablesExist(connectionString, username, password);
        EnsureInitialData(connectionString, username, password);
    }

    private static void EnsureDatabaseExists(string connectionString, string username, string password)
    {
        // Create a connection to the postgres database to check/create our database
        var masterConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres", // Connect to default postgres database first
            Username = username,
            Password = password
        };
        
        var dbName = "Todos";
        
        // Check if database exists
        using var masterConnection = new NpgsqlConnection(masterConnectionStringBuilder.ToString());
        masterConnection.Open();
        
        // Check if database exists
        using var checkCommand = masterConnection.CreateCommand();
        checkCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName";
        checkCommand.Parameters.AddWithValue("dbName", dbName);
        
        var dbExists = checkCommand.ExecuteScalar() != null;
        
        if (!dbExists)
        {
            // Create the database
            using var createDbCommand = masterConnection.CreateCommand();
            createDbCommand.CommandText = $"CREATE DATABASE \"{dbName}\"";
            createDbCommand.ExecuteNonQuery();
            
            Console.WriteLine($"Database '{dbName}' created successfully.");
        }
    }

    private static void EnsureTablesExist(string connectionString, string username, string password)
    {
        var todosConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Username = username,
            Password = password
        };
        
        using var connection = new NpgsqlConnection(todosConnectionStringBuilder.ToString());
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Todos (
                Id SERIAL PRIMARY KEY,
                Title VARCHAR(100) NOT NULL,
                IsComplete BOOLEAN NOT NULL DEFAULT FALSE
            )";
        command.ExecuteNonQuery();
    }

    private static void EnsureInitialData(string connectionString, string username, string password)
    {
        var todosConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Username = username,
            Password = password
        };
        
        using var connection = new NpgsqlConnection(todosConnectionStringBuilder.ToString());
        connection.Open();
        
        // Insert initial records if they don't exist
        using var checkRecordsCommand = connection.CreateCommand();
        checkRecordsCommand.CommandText = "SELECT COUNT(*) FROM Todos";
        var recordCount = Convert.ToInt32(checkRecordsCommand.ExecuteScalar());
        
        if (recordCount == 0)
        {
            using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO Todos (Title, IsComplete) VALUES
                ('Give the dog a bath', false),
                ('Wash the dishes', false),
                ('Do the groceries', false)
            ";
            insertCommand.ExecuteNonQuery();
            
            Console.WriteLine("Initial todo items added successfully.");
        }
    }
}
```

In the EndpointExtensions.cs file add:

```
public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
{
	app.MapGet("/weatherforecast", GetWeatherforecast).WithName("GetWeatherForecast");
	app.MapGet("/todos", GetTodos).WithName("GetTodos");
	app.MapGet("/todos/{id}", GetTodo).WithName("GetTodo");

	return app;
}

private static async Task<IResult> GetTodo(int id, NpgsqlConnection db)
{
	const string sql = """
		SELECT Id, Title, IsComplete
		FROM Todos
		WHERE Id = @id
		""";
	
	return await db.QueryFirstOrDefaultAsync<Todo>(sql, new { id }) is { } todo
		? Results.Ok(todo)
		: Results.NotFound();
}

private static async Task<IEnumerable<Todo>> GetTodos(NpgsqlConnection db)
{
	const string sql = """
		SELECT Id, Title, IsComplete
		FROM Todos
		""";

	return await db.QueryAsync<Todo>(sql);
}
```

At the bottom also add

```
public record Todo(int Id, string Title, bool IsComplete);
```

## Cloud deployment

https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/aca-deployment-azd-in-depth?tabs=macos

```
brew tap azure/azd && brew install azd
```

When performin azd it will request the name of the environment, put in a name **without** rg- in front!

First initialize the environment

```
azd init
```

Secondly upload the aspire project

```
azd up
```

When you do new updates, you do not need to run the full setup anymore. Only a deploy will be enough

```
azd deploy
```

If you want to tear down the full azure setup run

```
azd down
```

To view the actual bicep generated files run the following commands

```
azd config set alpha.infraSynth on
azd infra synth
```
