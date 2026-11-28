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
        SeedCredentials.EnvironmentVariable,
        SeedCredentials.DemoDataEnabledEnvironmentVariable);

    public SeedCredentialsTests()
    {
        _environment.Clear(SeedCredentials.EnabledEnvironmentVariable);
        _environment.Clear(SeedCredentials.EnvironmentVariable);
        _environment.Clear(SeedCredentials.DemoDataEnabledEnvironmentVariable);
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

    [Fact]
    public void ResolveDemoDataEnabled_DefaultsToFalse()
    {
        Assert.False(SeedCredentials.ResolveDemoDataEnabled(CreateConfiguration()));
    }

    [Fact]
    public void ResolveDemoDataEnabled_ConfigurationCanOptIn()
    {
        var configuration = CreateConfiguration((SeedCredentials.DemoDataEnabledConfigurationKey, "true"));

        Assert.True(SeedCredentials.ResolveDemoDataEnabled(configuration));
    }

    [Fact]
    public void ResolveDemoDataEnabled_EnvironmentOverridesConfiguration()
    {
        _environment.Set(SeedCredentials.DemoDataEnabledEnvironmentVariable, "false");
        var configuration = CreateConfiguration((SeedCredentials.DemoDataEnabledConfigurationKey, "true"));

        Assert.False(SeedCredentials.ResolveDemoDataEnabled(configuration));
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(value => value.Key, value => value.Value))
            .Build();
}
