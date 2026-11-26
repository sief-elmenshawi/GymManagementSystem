using Xunit;

namespace GymManagement.Tests.Support;

// License tests change process environment variables, so they must not run in parallel.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentVariableCollection
{
    public const string Name = "Environment variables";
}
