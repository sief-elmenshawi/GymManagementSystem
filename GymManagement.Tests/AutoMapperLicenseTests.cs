using GymManagement.Tests.Support;
using GymManagementPL;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymManagement.Tests;

[Collection(EnvironmentVariableCollection.Name)]
public sealed class AutoMapperLicenseTests : IDisposable
{
    private const string WellFormedKey = "eyJhbGciOiJub25lIn0.eyJzdWIiOiJ0ZXN0In0.c2lnbmF0dXJl";

    private readonly EnvironmentVariableScope _environment = new(
        AutoMapperLicense.AutoMapperEnvironmentVariable,
        AutoMapperLicense.LuckyPennyEnvironmentVariable);

    public AutoMapperLicenseTests()
    {
        _environment.Clear(AutoMapperLicense.AutoMapperEnvironmentVariable);
        _environment.Clear(AutoMapperLicense.LuckyPennyEnvironmentVariable);
    }

    public void Dispose() => _environment.Dispose();

    [Fact]
    public void ResolveKey_ConfigurationValueWinsOverEnvironmentVariables()
    {
        _environment.Set(AutoMapperLicense.AutoMapperEnvironmentVariable, "env-key");
        _environment.Set(AutoMapperLicense.LuckyPennyEnvironmentVariable, "shared-key");

        var configuration = CreateConfiguration(("AutoMapper:LicenseKey", "config-key"));

        Assert.Equal("config-key", AutoMapperLicense.ResolveKey(configuration));
        Assert.True(AutoMapperLicense.IsConfigured(configuration));
    }

    [Fact]
    public void ResolveKey_FallsBackToAutoMapperEnvironmentVariable()
    {
        _environment.Set(AutoMapperLicense.AutoMapperEnvironmentVariable, "env-key");
        _environment.Set(AutoMapperLicense.LuckyPennyEnvironmentVariable, "shared-key");

        Assert.Equal("env-key", AutoMapperLicense.ResolveKey(CreateConfiguration()));
    }

    [Fact]
    public void ResolveKey_FallsBackToSharedLuckyPennyEnvironmentVariable()
    {
        _environment.Set(AutoMapperLicense.LuckyPennyEnvironmentVariable, "shared-key");

        Assert.Equal("shared-key", AutoMapperLicense.ResolveKey(CreateConfiguration()));
    }

    [Fact]
    public void ResolveKey_ReturnsNull_WhenNoUsableKeyExists()
    {
        var configuration = CreateConfiguration(("AutoMapper:LicenseKey", "   "));

        Assert.Null(AutoMapperLicense.ResolveKey(configuration));
        Assert.False(AutoMapperLicense.IsConfigured(configuration));
    }

    [Fact]
    public void RequireKey_ReturnsWellFormedKey()
    {
        var configuration = CreateConfiguration(("AutoMapper:LicenseKey", WellFormedKey));

        Assert.Equal(WellFormedKey, AutoMapperLicense.RequireKey(configuration, "Production"));
    }

    [Fact]
    public void RequireKey_Throws_WhenNoKeyIsConfigured()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => AutoMapperLicense.RequireKey(CreateConfiguration(), "Staging"));

        Assert.Contains("Staging", exception.Message, StringComparison.Ordinal);
        Assert.Contains(AutoMapperLicense.ConfigurationKey, exception.Message, StringComparison.Ordinal);
        Assert.Contains(AutoMapperLicense.AutoMapperEnvironmentVariable, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireKey_Throws_WhenKeyIsMalformed()
    {
        var configuration = CreateConfiguration(("AutoMapper:LicenseKey", "config-key"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => AutoMapperLicense.RequireKey(configuration, "Production"));

        Assert.Contains("well-formed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value))
            .Build();
}
