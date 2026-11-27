namespace GymManagement.Tests.Support;

/// <summary>
/// Scopes environment-variable mutations so tests restore the process state afterwards.
/// </summary>
internal sealed class EnvironmentVariableScope : IDisposable
{
    private readonly Dictionary<string, string?> _originalValues;

    public EnvironmentVariableScope(params string[] names)
    {
        _originalValues = names.ToDictionary(
            name => name,
            name => Environment.GetEnvironmentVariable(name),
            StringComparer.Ordinal);
    }

    public void Set(string name, string? value) => Environment.SetEnvironmentVariable(name, value);

    public void Clear(string name) => Set(name, null);

    public void Dispose()
    {
        foreach (var (name, value) in _originalValues)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
