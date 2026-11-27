using Xunit;

namespace GymManagement.Tests;

public sealed class CommittedConfigurationTests
{
    [Fact]
    public void DevelopmentAppsettings_DoesNotContainSeedPassword()
    {
        var path = FindRepositoryFile("GymManagement", "appsettings.Development.json");
        var contents = File.ReadAllText(path);

        Assert.DoesNotContain("SeedUsers", contents, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("P@ssw0rd", contents, StringComparison.Ordinal);
    }

    private static string FindRepositoryFile(params string[] relativeParts)
    {
        var relativePath = Path.Combine(relativeParts);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} above {AppContext.BaseDirectory}.");
    }
}
