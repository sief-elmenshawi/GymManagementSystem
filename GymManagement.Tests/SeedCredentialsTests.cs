using GymManagement.Tests.Support;
using GymManagementPL;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymManagement.Tests;

[Collection(EnvironmentVariableCollection.Name)]
public sealed class SeedCredentialsTests : IDisposable
{
    private readonly EnvironmentVariableScope _environment =
        new(SeedCredentials.EnvironmentVariable);

    public SeedCredentialsTests() => _environment.Clear(SeedCredentials.EnvironmentVariable);

    public void Dispose() => _environment.Dispose();

    [Fact]
    public void ResolvePassword_EnvironmentValueWinsOverConfiguration()
    {
        _environment.Set(SeedCredentials.EnvironmentVariable, "env-password");

        var configuration = CreateConfiguration(("SeedUsers:Password", "config-password"));

        Assert.Equal("env-password", SeedCredentials.ResolvePassword(configuration));
    }

    [Fact]
    public void ResolvePassword_FallsBackToConfiguration()
    {
        var configuration = CreateConfiguration(("SeedUsers:Password", "config-password"));

        Assert.Equal("config-password", SeedCredentials.ResolvePassword(configuration));
    }

    [Fact]
    public void ResolvePassword_ReturnsNull_WhenNoUsablePasswordExists()
    {
        _environment.Set(SeedCredentials.EnvironmentVariable, "   ");
        var configuration = CreateConfiguration(("SeedUsers:Password", "  "));

        Assert.Null(SeedCredentials.ResolvePassword(configuration));
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value))
            .Build();
}
