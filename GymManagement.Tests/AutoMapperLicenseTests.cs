using GymManagement.Tests.Support;
using GymManagementPL;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymManagement.Tests;

[Collection(EnvironmentVariableCollection.Name)]
public sealed class AutoMapperLicenseTests
{
    public AutoMapperLicenseTests()
    {
        ClearEnvironmentVariables();
    }

    [Fact]
    public void ResolveKey_ConfigurationValueWinsOverEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable(AutoMapperLicense.AutoMapperEnvironmentVariable, "env-key");
        Environment.SetEnvironmentVariable(AutoMapperLicense.LuckyPennyEnvironmentVariable, "shared-key");

        var configuration = CreateConfiguration(("AutoMapper:LicenseKey", "config-key"));

        Assert.Equal("config-key", AutoMapperLicense.ResolveKey(configuration));
        Assert.True(AutoMapperLicense.IsConfigured(configuration));
    }

    [Fact]
    public void ResolveKey_FallsBackToAutoMapperEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(AutoMapperLicense.AutoMapperEnvironmentVariable, "env-key");
        Environment.SetEnvironmentVariable(AutoMapperLicense.LuckyPennyEnvironmentVariable, "shared-key");

        var configuration = CreateConfiguration();

        Assert.Equal("env-key", AutoMapperLicense.ResolveKey(configuration));
    }

    [Fact]
    public void ResolveKey_FallsBackToSharedLuckyPennyEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(AutoMapperLicense.LuckyPennyEnvironmentVariable, "shared-key");

        var configuration = CreateConfiguration();

        Assert.Equal("shared-key", AutoMapperLicense.ResolveKey(configuration));
    }

    [Fact]
    public void ResolveKey_ReturnsNull_WhenNoUsableKeyExists()
    {
        var configuration = CreateConfiguration(("AutoMapper:LicenseKey", "   "));

        Assert.Null(AutoMapperLicense.ResolveKey(configuration));
        Assert.False(AutoMapperLicense.IsConfigured(configuration));
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value))
            .Build();

    private static void ClearEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable(AutoMapperLicense.AutoMapperEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(AutoMapperLicense.LuckyPennyEnvironmentVariable, null);
    }
}
