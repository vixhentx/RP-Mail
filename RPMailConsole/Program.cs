using System.CommandLine;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RPMailCore.Models;
using RPMailCore.Serialization;
using RPMailCore.ViewModels;

namespace RPMailConsole;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var root = new RootCommand("A console application to send EMail automatically, with Scriban templates like \"{{ Text }}\" and HTML attachment rendered into PDF.");

        var configOpt = new Option<string>("config", ["-c", "--config"]) { Description = "Path to the JSON config file ('-' to read from stdin)" };
        var templateOpt = new Option<bool>("template", ["-t", "--template"]) { Description = "Print a config template to stdout and exit" };
        var quietOpt = new Option<bool>("quiet", ["-q", "--quiet"]) { Description = "Quiet mode" };
        var versionOpt = new Option<bool>("version", ["--version"]) { Description = "Show version information" };

        root.Options.Add(configOpt);
        root.Options.Add(templateOpt);
        root.Options.Add(quietOpt);
        root.Options.Add(versionOpt);

        root.SetAction(async (ParseResult pr, CancellationToken ct) =>
        {
            if (pr.GetValue(versionOpt))
            {
                Console.WriteLine(typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");
                return 0;
            }

            if (pr.GetValue(templateOpt))
            {
                Console.WriteLine(JsonSerializer.Serialize(MailConfig.CreateTemplate(), RPMailJsonContext.Default.MailConfig));
                return 0;
            }

            bool quiet = pr.GetValue(quietOpt);
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(quiet ? LogLevel.None : LogLevel.Information);
                if (!quiet)
                    builder.AddSimpleConsole();
            });

            if (pr.GetValue(configOpt) is not { Length: > 0 } configPath)
            {
                Console.Error.WriteLine("Missing required option: -c/--config (or use -t/--template to generate a config template).");
                return 2;
            }

            try
            {
                string json = configPath == "-"
                    ? await Console.In.ReadToEndAsync(ct)
                    : await File.ReadAllTextAsync(configPath, ct);

                var config = JsonSerializer.Deserialize(json, RPMailJsonContext.Default.MailConfig)
                    ?? throw new JsonException("JSON config is null or empty.");

                var vm = new MailViewModel(loggerFactory.CreateLogger("RPMail"));
                try
                {
                    var result = await vm.RunAsync(config, ct);
                    if (!result.Success)
                    {
                        Console.Error.WriteLine(result.FatalException?.Message);
                        return 2;
                    }
                    return result.FailedRows > 0 ? 1 : 0;
                }
                finally
                {
                    vm.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Failed to load config: {e.Message}");
                return 2;
            }
        });

        var parseResult = root.Parse(args, new ParserConfiguration());
        return await parseResult.InvokeAsync(new InvocationConfiguration());
    }
}
