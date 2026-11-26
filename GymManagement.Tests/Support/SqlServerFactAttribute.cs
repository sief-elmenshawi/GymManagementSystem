using Xunit;

namespace GymManagement.Tests.Support;

/// <summary>
/// Runs only when GYM_TEST_CONNECTION_STRING points at a disposable SQL Server test database.
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING")))
        {
            Skip = "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.";
        }
    }
}
