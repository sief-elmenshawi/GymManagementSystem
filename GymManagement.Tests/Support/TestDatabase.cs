using Microsoft.Data.SqlClient;

namespace GymManagement.Tests.Support;

internal static class TestDatabase
{
    public static string ToTestConnectionString(string connectionString, string databaseName) =>
        new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName }.ConnectionString;

    public static async Task DropAsync(string connectionString, string databaseName)
    {
        var masterConnection = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
        };

        await using var connection = new SqlConnection(masterConnection.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN " +
            $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            $"DROP DATABASE [{databaseName}]; END";
        await command.ExecuteNonQueryAsync();
    }
}
