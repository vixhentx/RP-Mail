using System.Text;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using RPMailCore.Services;

namespace RPMailCore.Processors;

public sealed class HtmlToPdfProcessor(bool saveRawHtml) : IAsyncDisposable
{
    private readonly Lazy<Task<IBrowser>> _browser = new(LaunchBrowserAsync);
    private bool _disposed;

    public async Task ConvertAsync(string renderedHtml, string outputPdfPath, Encoding encoding)
    {
        var browser = await _browser.Value;
        string htmlFile = outputPdfPath + ".html";
        FileIo.WriteAllText(htmlFile, renderedHtml, encoding);
        try
        {
            await using var page = await browser.NewPageAsync();
            await page.GoToAsync(new Uri(Path.GetFullPath(htmlFile)).AbsoluteUri);
            await page.PdfAsync(outputPdfPath, new PdfOptions
            {
                Format = new PaperFormat(8.27m, 11.69m),
                PrintBackground = true,
            });
        }
        finally
        {
            if (!saveRawHtml)
                FileIo.Delete(htmlFile);
        }
    }

    private static Task<IBrowser> LaunchBrowserAsync() =>
        Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = ResolveExecutablePath(),
        });

    private static string ResolveExecutablePath()
    {
        string? fromEnv = Environment.GetEnvironmentVariable("RPMAIL_CHROMIUM_PATH");
        if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;

        if (OperatingSystem.IsWindows())
        {
            foreach (var candidate in new[]
            {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            })
            {
                if (File.Exists(candidate)) return candidate;
            }
        }
        else
        {
            foreach (var name in new[] { "chromium", "google-chrome", "chromium-browser" })
            {
                string? found = FindOnPath(name);
                if (found is not null) return found;
            }
        }

        return "chromium";
    }

    private static string? FindOnPath(string name)
    {
        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar)) return null;
        foreach (string dir in pathVar.Split(Path.PathSeparator))
        {
            string full = Path.Combine(dir, name);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_browser.IsValueCreated)
        {
            var browser = await _browser.Value;
            await browser.CloseAsync();
        }
    }
}
