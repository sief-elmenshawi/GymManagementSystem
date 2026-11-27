using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright;

namespace GymManagement.Tests.Support;

/// <summary>
/// Runs the real web app on a local Kestrel port and exposes a Chromium page that
/// drives rendered HTML. Used only by opt-in browser tests.
/// </summary>
internal sealed class BrowserTestApp : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly StringBuilder _processOutput = new();
    private Process? _process;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public BrowserTestApp(string connectionString, string databaseName)
    {
        _connectionString = connectionString;
        _databaseName = databaseName;
    }

    public string BaseUrl { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        await TestDatabase.DropAsync(_connectionString, _databaseName);

        var port = ReserveFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var applicationDirectory = Path.GetDirectoryName(typeof(GymManagementPL.Program).Assembly.Location)
            ?? throw new InvalidOperationException("Could not locate the built web application.");
        var applicationAssembly = Path.Combine(applicationDirectory, "GymManagement.PL.dll");
        var contentRoot = FindContentRoot(applicationDirectory);

        var startInfo = new ProcessStartInfo("dotnet")
        {
            Arguments = $"\"{applicationAssembly}\"",
            WorkingDirectory = applicationDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_CONTENTROOT"] = contentRoot;
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        startInfo.Environment["ConnectionStrings__DefaultConnection"] =
            TestDatabase.ToTestConnectionString(_connectionString, _databaseName);
        startInfo.Environment["SeedUsers__Password"] = "Test@12345";

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the web application process.");
        _process.OutputDataReceived += (_, args) => AppendOutput(args.Data);
        _process.ErrorDataReceived += (_, args) => AppendOutput(args.Data);
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitForApplicationAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async Task<IPage> NewPageAsync()
    {
        if (_browser is null)
        {
            throw new InvalidOperationException("The browser fixture has not been started.");
        }

        var context = await _browser.NewContextAsync();
        return await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_process is not null)
        {
            if (!_process.HasExited)
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the check and the kill.
                }
            }

            await _process.WaitForExitAsync();
            _process.Dispose();
        }

        await TestDatabase.DropAsync(_connectionString, _databaseName);
    }

    private void AppendOutput(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        lock (_processOutput)
        {
            _processOutput.AppendLine(line);
            if (_processOutput.Length > 32_000)
            {
                _processOutput.Remove(0, _processOutput.Length - 32_000);
            }
        }
    }

    private async Task WaitForApplicationAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        while (!timeout.IsCancellationRequested)
        {
            if (_process!.HasExited)
            {
                throw new InvalidOperationException(
                    $"The web application exited with code {_process.ExitCode}. " +
                    GetRecentOutput());
            }

            try
            {
                using var response = await client.GetAsync("/Account/Login", timeout.Token);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
                // The server is not listening yet.
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException(
            "The web application did not become ready in time. " + GetRecentOutput());
    }

    private string GetRecentOutput()
    {
        lock (_processOutput)
        {
            var lines = _processOutput.ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            return "Recent application output: " +
                   string.Join(" | ", lines.TakeLast(12));
        }
    }

    private static string FindContentRoot(string applicationDirectory)
    {
        var candidate = new DirectoryInfo(applicationDirectory);
        while (candidate is not null)
        {
            var direct = Path.Combine(candidate.FullName, "wwwroot", "Files", "plans.json");
            if (File.Exists(direct)) return candidate.FullName;

            // Project references copy the app assembly into the test output folder;
            // the content root is then the sibling web project.
            var sibling = Path.Combine(candidate.FullName, "GymManagement", "wwwroot", "Files", "plans.json");
            if (File.Exists(sibling)) return Path.Combine(candidate.FullName, "GymManagement");

            candidate = candidate.Parent;
        }

        throw new InvalidOperationException(
            "Could not find the web application content root containing wwwroot/Files/plans.json.");
    }

    private static int ReserveFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
