using GymManagement.Tests.Support;
using GymManagementPL;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymManagement.Tests;

[Collection(EnvironmentVariableCollection.Name)]
public sealed class SeedCredentialsTests : IDisposable
{
    private readonly EnvironmentVariableScope _environment = new(
        SeedCredentials.EnabledEnvironmentVariable,
        SeedCredentials.EnvironmentVariable);

    public SeedCredentialsTests()
    {
        _environment.Clear(SeedCredentials.EnabledEnvironmentVariable);
        _environment.Clear(SeedCredentials.EnvironmentVariable);
    }

    public void Dispose() => _environment.Dispose();

    [Fact]
    public void ResolveUserSeedingEnabled_DefaultsToDevelopmentOnly()
    {
        Assert.True(SeedCredentials.ResolveUserSeedingEnabled(
            CreateConfiguration(), isDevelopment: true));
        Assert.False(SeedCredentials.ResolveUserSeedingEnabled(
            CreateConfiguration(), isDevelopment: false));
    }

    [Fact]
    public void ResolveUserSeedingEnabled_ConfigurationCanOptInOutsideDevelopment()
    {
        var configuration = CreateConfiguration((SeedCredentials.EnabledConfigurationKey, "true"));

        Assert.True(SeedCredentials.ResolveUserSeedingEnabled(configuration, isDevelopment: false));
    }

    [Fact]
    public void ResolveUserSeedingEnabled_EnvironmentOverridesConfiguration()
    {
        _environment.Set(SeedCredentials.EnabledEnvironmentVariable, "false");
        var configuration = CreateConfiguration((SeedCredentials.EnabledConfigurationKey, "true"));

        Assert.False(SeedCredentials.ResolveUserSeedingEnabled(configuration, isDevelopment: false));
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(value => value.Key, value => value.Value))
            .Build();
}
