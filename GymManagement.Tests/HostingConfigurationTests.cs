using GymManagementPL;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymManagement.Tests;

public sealed class HostingConfigurationTests
{
    [Theory]
    [InlineData("https://+:443")]
    [InlineData("http://+:80;https://+:443")]
    [InlineData("HTTPS://example.com")]
    public void ShouldUseHttpsRedirection_FromUrls(string urls)
    {
        var configuration = CreateConfiguration(("urls", urls));

        Assert.True(HostingConfiguration.ShouldUseHttpsRedirection(configuration));
    }

    [Fact]
    public void ShouldUseHttpsRedirection_False_ForHttpOnlyUrls()
    {
        var configuration = CreateConfiguration(("urls", "http://+:80"));

        Assert.False(HostingConfiguration.ShouldUseHttpsRedirection(configuration));
    }

    [Theory]
    [InlineData("https_port")]
    [InlineData("HTTPS_PORT")]
    [InlineData("HttpsPort")]
    public void ShouldUseHttpsRedirection_FromStandardAndLegacyPortKeys(string key)
    {
        var configuration = CreateConfiguration((key, "5001"));

        Assert.Equal(5001, HostingConfiguration.GetHttpsPort(configuration));
        Assert.True(HostingConfiguration.ShouldUseHttpsRedirection(configuration));
    }

    [Fact]
    public void GetHttpsPort_IgnoresInvalidValues()
    {
        var configuration = CreateConfiguration(("https_port", "not-a-port"));

        Assert.Null(HostingConfiguration.GetHttpsPort(configuration));
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value))
            .Build();
}
