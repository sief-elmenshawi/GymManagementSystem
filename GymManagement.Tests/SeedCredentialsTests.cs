using GymManagement.Tests.Support;
using GymManagementPL;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymManagement.Tests;

[Collection(EnvironmentVariableCollection.Name)]
public sealed class SeedCredentialsTests
{
    public SeedCredentialsTests()
    {
        Environment.SetEnvironmentVariable(SeedCredentials.EnvironmentVariable, null);
    }

    [Fact]
    public void ResolvePassword_ConfigurationValueWinsOverEnvironment()
    {
        Environment.SetEnvironmentVariable(SeedCredentials.EnvironmentVariable, "env-password");

        var configuration = CreateConfiguration(("SeedUsers:Password", "config-password"));

        Assert.Equal("config-password", SeedCredentials.ResolvePassword(configuration));
    }

    [Fact]
    public void ResolvePassword_FallsBackToEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(SeedCredentials.EnvironmentVariable, "env-password");

        Assert.Equal("env-password", SeedCredentials.ResolvePassword(CreateConfiguration()));
    }

    [Fact]
    public void ResolvePassword_ReturnsNull_WhenNoUsablePasswordExists()
    {
        var configuration = CreateConfiguration(("SeedUsers:Password", "  "));

        Assert.Null(SeedCredentials.ResolvePassword(configuration));
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value))
            .Build();
}
