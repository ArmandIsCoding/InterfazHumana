using Microsoft.Data.Sqlite;

namespace InterfazHumana.IO.Data;

public sealed class DatabaseContext
{
    public DatabaseContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("El connection string no puede estar vacío.", nameof(connectionString));
        }

        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCommand.Prepare();
        pragmaCommand.ExecuteNonQuery();

        return connection;
    }
}

