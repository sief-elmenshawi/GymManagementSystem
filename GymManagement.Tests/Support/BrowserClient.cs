using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace GymManagement.Tests.Support;

/// <summary>
/// Minimal browser-like client: keeps antiforgery/identity cookies and posts real HTML forms.
/// </summary>
internal sealed class BrowserClient : IDisposable
{
    private readonly HttpClient _client;
    private readonly Dictionary<string, string> _cookies = new();

    public BrowserClient(HttpClient client) => _client = client;

    public void Dispose() => _client.Dispose();

    public async Task<string> GetStringAsync(string url)
    {
        using var response = await GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        AssertSuccess(response, body, url);
        return body;
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyCookies(request);
        var response = await _client.SendAsync(request);
        CaptureCookies(response);
        return response;
    }

    public async Task<HttpResponseMessage> PostFormAsync(
        string url,
        IEnumerable<KeyValuePair<string, string>> fields)
    {
        using var content = new FormUrlEncodedContent(fields);
        return await PostAsync(url, content);
    }

    public async Task<HttpResponseMessage> PostMultipartAsync(
        string url,
        IEnumerable<KeyValuePair<string, string>> fields,
        string fileField,
        string fileName,
        byte[] fileBytes,
        string contentType = "image/png")
    {
        using var content = new MultipartFormDataContent();
        foreach (var field in fields)
        {
            content.Add(new StringContent(field.Value), field.Key);
        }

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = $"\"{fileField}\"",
            FileName = $"\"{fileName}\"",
        };
        content.Add(fileContent);

        return await PostAsync(url, content);
    }

    public static string ExtractToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            throw new InvalidOperationException("Antiforgery token was not found in the response.");
        }

        return match.Groups[1].Value;
    }

    private async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        ApplyCookies(request);
        var response = await _client.SendAsync(request);
        CaptureCookies(response);
        return response;
    }

    private void ApplyCookies(HttpRequestMessage request)
    {
        if (_cookies.Count == 0) return;
        request.Headers.TryAddWithoutValidation(
            "Cookie",
            string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
    }

    private void CaptureCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        foreach (var header in values)
        {
            var pair = header.Split(';')[0].Split('=', 2);
            if (pair.Length == 2)
            {
                _cookies[pair[0]] = pair[1];
            }
        }
    }

    private static void AssertSuccess(HttpResponseMessage response, string body, string url)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"GET {url} returned {(int)response.StatusCode}: {body[..Math.Min(500, body.Length)]}");
        }
    }
}
