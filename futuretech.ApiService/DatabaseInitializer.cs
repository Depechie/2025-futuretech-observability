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