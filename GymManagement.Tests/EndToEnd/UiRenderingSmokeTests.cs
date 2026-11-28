using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using GymManagement.Tests.Support;
using Microsoft.Playwright;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Opt-in rendering audit: walks every linked GET page for a SuperAdmin and for a
/// member, then checks status, images, overflow, accessible names and console errors.
/// </summary>
public sealed class UiRenderingSmokeTests
{
    private const string AuditScript = """
        window.__gymAudit = () => {
            const issues = [];
            const doc = document;
            const width = window.innerWidth;

            if (doc.documentElement.scrollWidth > width + 2) {
                issues.push('horizontal overflow: ' + doc.documentElement.scrollWidth + 'px > ' + width + 'px');
                const wide = Array.from(doc.querySelectorAll('body *'))
                    .map(el => ({ el, rect: el.getBoundingClientRect() }))
                    .filter(item => item.rect.right > width + 2 && item.rect.width > 0)
                    .sort((a, b) => b.rect.right - a.rect.right)
                    .slice(0, 5);
                for (const item of wide) {
                    const el = item.el;
                    issues.push('overflow source: ' + el.tagName.toLowerCase() +
                        (el.id ? '#' + el.id : '') +
                        (el.className && typeof el.className === 'string'
                            ? '.' + el.className.trim().split(/\\s+/).slice(0, 2).join('.')
                            : '') +
                        ' right=' + Math.round(item.rect.right) +
                        ' width=' + Math.round(item.rect.width));
                }
            }

            for (const img of Array.from(doc.images)) {
                const src = img.currentSrc || img.getAttribute('src') || '(no src)';
                const hidden = img.offsetParent === null ||
                    window.getComputedStyle(img).display === 'none';
                if (!img.hasAttribute('alt')) issues.push('image without alt: ' + src);
                if (hidden) continue;
                if (!src || src === '(no src)' || src.endsWith('#')) continue;
                if (img.complete && img.naturalWidth === 0) issues.push('broken image: ' + src);
            }

            const idCounts = {};
            for (const el of Array.from(doc.querySelectorAll('[id]'))) {
                idCounts[el.id] = (idCounts[el.id] || 0) + 1;
            }
            for (const id of Object.keys(idCounts)) {
                if (idCounts[id] > 1) issues.push('duplicate id: ' + id);
            }

            const controls = Array.from(doc.querySelectorAll(
                'input:not([type=hidden]), select, textarea, button'));
            for (const el of controls) {
                if (el.getAttribute('aria-hidden') === 'true') continue;
                const rect = el.getBoundingClientRect();
                if (rect.width === 0 && rect.height === 0) continue;

                let name = (el.getAttribute('aria-label') || '').trim();
                const labelledBy = el.getAttribute('aria-labelledby');
                if (!name && labelledBy) {
                    name = labelledBy.split(/\s+/)
                        .map(id => (doc.getElementById(id)?.textContent || '').trim())
                        .join(' ').trim();
                }
                if (!name && el.id) {
                    name = Array.from(doc.querySelectorAll('label[for="' + CSS.escape(el.id) + '"]'))
                        .map(label => label.textContent.trim()).join(' ').trim();
                }
                if (!name && el.closest('label')) {
                    name = el.closest('label').textContent.trim();
                }
                if (!name) name = (el.getAttribute('title') || '').trim();
                if (!name && (el.tagName === 'BUTTON' || el.type === 'submit' || el.type === 'button')) {
                    name = (el.textContent || el.value || '').trim();
                }
                if (!name) {
                    issues.push('control without accessible name: ' + el.tagName +
                        ' ' + (el.getAttribute('name') || el.className || el.outerHTML.slice(0, 60)));
                }
            }

            return issues;
        };
        """;

    private const string Password = "Test@12345";
    private const string SuperAdminEmail = "Serag@gmail.com";

    private static readonly string[] PublicRoutes =
    [
        "/",
        "/Account/Login", "/Account/Register", "/Account/ForgotPassword",
        "/Account/ForgotPasswordConfirmation", "/Account/ResendEmailConfirmation",
        "/Account/ResendEmailConfirmationConfirmation", "/Account/ResetPasswordConfirmation",
        "/Account/ConfirmEmail", "/Account/ResetPassword", "/Account/AccessDenied",
    ];

    private static readonly string[] AuthenticatedRoutes =
    [
        "/Home/Privacy", "/Home/Error",
    ];

    private static readonly string[] SkipFragments =
    [
        "/Account/Logout", "/Account/ConfirmEmail", "/dev-mailbox",
        "/Activate", "/Cancel", "/Attended", "/Book", "/Picture",
        "/ResetPassword", "javascript:", "mailto:", "tel:",
    ];

    [BrowserFact]
    public async Task EveryLinkedPage_RendersCleanly()
    {
        var connectionString = Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING")!;
        var app = new BrowserTestApp(connectionString, $"GymManagement_E2E_UiAudit_{Guid.NewGuid():N}");
        await app.StartAsync();

        try
        {
            var page = await app.NewPageAsync();
            await page.SetViewportSizeAsync(1440, 900);
            await page.AddInitScriptAsync(AuditScript);
            var problems = new List<string>();

            // Public pages first.
            foreach (var route in PublicRoutes)
            {
                problems.AddRange(await AuditRouteAsync(page, app.BaseUrl, route));
            }

            await page.SetViewportSizeAsync(390, 844);
            foreach (var route in PublicRoutes.Take(6))
            {
                problems.AddRange(await AuditRouteAsync(page, app.BaseUrl, route));
            }
            await page.SetViewportSizeAsync(1440, 900);

            // Staff pages, including authenticated utility pages and every safe GET link found.
            await LoginAsync(page, app.BaseUrl, SuperAdminEmail, Password);
            foreach (var route in AuthenticatedRoutes)
            {
                problems.AddRange(await AuditRouteAsync(page, app.BaseUrl, route));
            }

            var staffRoutes = await DiscoverAsync(
                page, app.BaseUrl,
                ["/Dashboard", "/Members/Index", "/Trainers/Index", "/Sessions/Index",
                 "/Plans/Index", "/Membership/Index", "/Booking/Index", "/Users"],
                80);
            foreach (var route in staffRoutes)
            {
                problems.AddRange(await AuditRouteAsync(page, app.BaseUrl, route));
            }

            await page.SetViewportSizeAsync(390, 844);
            foreach (var route in staffRoutes.Take(6))
            {
                problems.AddRange(await AuditRouteAsync(page, app.BaseUrl, route));
            }
            await page.SetViewportSizeAsync(1440, 900);

            // Member portal pages.
            var memberEmail = $"ui-audit-{Guid.NewGuid():N}@example.com";
            await RegisterMemberAsync(page, app.BaseUrl, memberEmail);
            await ConfirmMemberAsync(page, app, memberEmail);
            await LoginAsync(page, app.BaseUrl, memberEmail, Password);
            var memberRoutes = await DiscoverAsync(
                page, app.BaseUrl, ["/Member", "/Member/EditProfile", "/Member/Sessions"], 12);
            foreach (var route in memberRoutes)
            {
                problems.AddRange(await AuditRouteAsync(page, app.BaseUrl, route));
            }

            await page.SetViewportSizeAsync(390, 844);
            foreach (var route in memberRoutes.Take(4))
            {
                problems.AddRange(await AuditRouteAsync(page, app.BaseUrl, route));
            }

            Assert.True(
                problems.Count == 0,
                $"Rendering audit found {problems.Count} problem(s):{Environment.NewLine}" +
                string.Join(Environment.NewLine, problems.Distinct().Take(80)));
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    private static async Task<IReadOnlyList<string>> DiscoverAsync(
        IPage page,
        string baseUrl,
        IEnumerable<string> roots,
        int maxPages)
    {
        var queue = new Queue<string>(roots.Select(Normalize));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var routes = new List<string>();

        while (queue.Count > 0 && routes.Count < maxPages)
        {
            var path = queue.Dequeue();
            if (!seen.Add(path) || IsSkipped(path)) continue;

            var response = await page.GotoAsync(baseUrl + path, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
            });
            if (response is null || !response.Ok || IsLoginRedirect(page.Url, path)) continue;

            routes.Add(path);

            var hrefs = await page.Locator("a[href]").EvaluateAllAsync<string[]>(
                "els => els.map(el => el.getAttribute('href')).filter(Boolean)");
            foreach (var href in hrefs)
            {
                if (!href.StartsWith('/') || href.StartsWith("//")) continue;
                var candidate = Normalize(href.Split('#')[0]);
                if (!seen.Contains(candidate) && !IsSkipped(candidate))
                {
                    queue.Enqueue(candidate);
                }
            }
        }

        return routes;
    }

    private static async Task<List<string>> AuditRouteAsync(IPage page, string baseUrl, string route)
    {
        var issues = new List<string>();
        var consoleErrors = new List<string>();

        void OnConsole(object? sender, IConsoleMessage message)
        {
            if (message.Type == "error")
            {
                consoleErrors.Add(message.Text);
            }
        }

        void OnPageError(object? sender, string error) => consoleErrors.Add(error);

        page.Console += OnConsole;
        page.PageError += OnPageError;

        try
        {
            var response = await page.GotoAsync(baseUrl + route, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
            });

            if (response is null || !response.Ok)
            {
                issues.Add($"{route}: HTTP {(response?.Status ?? 0)}");
                return issues;
            }

            if (IsLoginRedirect(page.Url, route))
            {
                issues.Add($"{route}: redirected to login");
                return issues;
            }

            try
            {
                await page.WaitForFunctionAsync(
                    "() => Array.from(document.images).every(img => img.complete)",
                    null,
                    new PageWaitForFunctionOptions { Timeout = 4000 });
            }
            catch (PlaywrightException)
            {
                issues.Add($"{route}: images did not finish loading");
            }

            var found = await page.EvaluateAsync<string[]>("() => window.__gymAudit()");
            foreach (var item in found)
            {
                issues.Add($"{route}: {item}");
            }

            foreach (var error in consoleErrors.Distinct())
            {
                issues.Add($"{route}: console error: {Truncate(error)}");
            }
        }
        finally
        {
            page.Console -= OnConsole;
            page.PageError -= OnPageError;
        }

        return issues;
    }

    private static async Task LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync($"{baseUrl}/Account/Login");
        await page.Locator("input[name='Email']").FillAsync(email);
        await page.Locator("input[name='Password']").FillAsync(password);
        await page.Locator("button[type='submit']").ClickAsync();
        await page.WaitForURLAsync(new Regex("/(Home|Dashboard|Member)(/|$)"));
    }

    private static async Task RegisterMemberAsync(IPage page, string baseUrl, string email)
    {
        await page.GotoAsync($"{baseUrl}/Account/Register");
        await page.Locator("input[name='FirstName']").FillAsync("Rendering");
        await page.Locator("input[name='LastName']").FillAsync("Audit");
        await page.Locator("input[name='Email']").FillAsync(email);
        await page.Locator("input[name='Phone']").FillAsync("01012345678");
        await page.Locator("input[name='DateOfBirth']").FillAsync("1993-06-12");
        await page.Locator("select[name='Gender']").SelectOptionAsync("1");
        await page.Locator("input[name='Street']").FillAsync("Audit Street");
        await page.Locator("input[name='City']").FillAsync("Cairo");
        await page.Locator("input[name='BuildingNumber']").FillAsync("7");
        await page.Locator("input[name='Password']").FillAsync(Password);
        await page.Locator("input[name='ConfirmPassword']").FillAsync(Password);
        await page.Locator("button[type='submit']").ClickAsync();
        await page.WaitForURLAsync("**/Account/Login");
    }

    private static async Task ConfirmMemberAsync(IPage page, BrowserTestApp app, string email)
    {
        await using var mailbox = await page.Context.APIRequest.GetAsync(
            $"{app.BaseUrl}/dev-mailbox/{Uri.EscapeDataString(email)}");
        using var document = JsonDocument.Parse(await mailbox.TextAsync());

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var body = item.TryGetProperty("htmlBody", out var property)
                ? property.GetString()
                : null;
            if (string.IsNullOrEmpty(body)) continue;

            var match = Regex.Match(body, "href=\"([^\"]*Account/ConfirmEmail[^\"]*)\"", RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            await page.GotoAsync(WebUtility.HtmlDecode(match.Groups[1].Value));
            return;
        }

        throw new InvalidOperationException($"No confirmation link found for {email}.");
    }

    private static bool IsLoginRedirect(string url, string route) =>
        url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) &&
        !route.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase);

    private static bool IsSkipped(string path) =>
        SkipFragments.Any(fragment =>
            path.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string href)
    {
        if (string.IsNullOrWhiteSpace(href) || href == "#") return "/";
        var index = href.IndexOf('?', StringComparison.Ordinal);
        return index >= 0 ? href[..index] : href;
    }

    private static string Truncate(string value) =>
        value.Length <= 180 ? value : value[..180] + "…";
}
