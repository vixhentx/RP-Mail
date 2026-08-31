using System.CommandLine;
using System.Text;
using Microsoft.Extensions.Logging;
using RPMailCore.Models;
using RPMailCore.ViewModels;

namespace RPMailConsole;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var root = new RootCommand("A console application to send EMail automatically, with Scriban templates like \"{{ Text }}\" and HTML attachment rendered into PDF.");

        var senderOpt = new Option<string>("sender", ["-s", "--sender"]) { Description = "Sender email address", Required = true };
        var hostOpt = new Option<string>("host", ["--host"]) { Description = "SMTP Host & Port", Required = true };
        var passwordOpt = new Option<string>("password", ["-p", "--pwd", "--password"]) { Description = "Password", Required = true };
        var receiverHeaderOpt = new Option<string>("receiver-header", ["-r", "--receiver-header"]) { Description = "Receiver header in CSV file" };
        var csvOpt = new Option<string>("csv", ["-d", "--csv", "--data"]) { Description = "CSV Data File Path", Required = true };
        var subjectOpt = new Option<string>("subject", ["-t", "--title", "--subject"]) { Description = "Email Subject Pattern", Required = true };
        var bodyOpt = new Option<string>("body", ["-m", "-b", "--html", "--body", "--message"]) { Description = "HTML Email Body Pattern File Path", Required = true };
        var attachmentOpt = new Option<string[]>("attachment", ["-a", "--attachment"]) { Description = "HTML Attachment Pattern File Path", AllowMultipleArgumentsPerToken = true };
        var attachmentNameOpt = new Option<string[]>("attachment-name", ["-n", "--attachment-name"]) { Description = "Attachment Name Pattern", AllowMultipleArgumentsPerToken = true };
        var charsetOpt = new Option<string>("charset", ["-c", "--charset"]) { Description = "All Files Encoding if BOM absent" };
        var outputOpt = new Option<string>("output", ["-o", "--output"]) { Description = "Attachment Convert File Directory" };
        var deleteAfterConvertOpt = new Option<bool>("delete-after-convert", ["--delete-after-convert"]) { Description = "Delete Attachment Output after convert" };
        var quietOpt = new Option<bool>("quiet", ["-q", "--quiet"]) { Description = "Quiet mode" };
        var convertOnlyOpt = new Option<bool>("convert-only", ["--convert-only"]) { Description = "Only Convert Attachments and Exit" };
        var saveRawDocOpt = new Option<bool>("save-raw-doc", ["--save-raw-doc"]) { Description = "Save Rendered HTML in Output Directory" };
        var saveHtmlOpt = new Option<bool>("save-html", ["--save-html"]) { Description = "Save Rendered Email Body HTML in Output Directory" };
        var versionOpt = new Option<bool>("version", ["--version"]) { Description = "Show version information" };

        root.Options.Add(senderOpt);
        root.Options.Add(hostOpt);
        root.Options.Add(passwordOpt);
        root.Options.Add(receiverHeaderOpt);
        root.Options.Add(csvOpt);
        root.Options.Add(subjectOpt);
        root.Options.Add(bodyOpt);
        root.Options.Add(attachmentOpt);
        root.Options.Add(attachmentNameOpt);
        root.Options.Add(charsetOpt);
        root.Options.Add(outputOpt);
        root.Options.Add(deleteAfterConvertOpt);
        root.Options.Add(quietOpt);
        root.Options.Add(convertOnlyOpt);
        root.Options.Add(saveRawDocOpt);
        root.Options.Add(saveHtmlOpt);
        root.Options.Add(versionOpt);

        root.SetAction(async (ParseResult pr, CancellationToken ct) =>
        {
            if (pr.GetValue(versionOpt))
            {
                Console.WriteLine(typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");
                return 0;
            }

            bool quiet = pr.GetValue(quietOpt);
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(quiet ? LogLevel.None : LogLevel.Information);
                if (!quiet)
                    builder.AddSimpleConsole();
            });

            var vm = new MailViewModel(loggerFactory.CreateLogger("RPMail"));
            vm.CsvPath.Value = pr.GetValue(csvOpt)!;
            vm.HtmlPath.Value = pr.GetValue(bodyOpt)!;
            vm.Subject.Value = pr.GetValue(subjectOpt)!;
            vm.ReceiverHeader.Value = pr.GetValue(receiverHeaderOpt) is { Length: > 0 } header ? header : "Receiver";
            vm.CharSet.Value = pr.GetValue(charsetOpt) is { Length: > 0 } charset ? charset : "utf-8";
            vm.SmtpHost.Value = pr.GetValue(hostOpt)!;
            vm.SenderEmail.Value = pr.GetValue(senderOpt)!;
            vm.SenderPassword.Value = pr.GetValue(passwordOpt)!;
            vm.OutputDir.Value = pr.GetValue(outputOpt) is { Length: > 0 } output ? output : "Output";
            vm.SaveHtmlFile.Value = pr.GetValue(saveHtmlOpt);
            vm.SaveRawDocs.Value = pr.GetValue(saveRawDocOpt);
            vm.ConvertOnly.Value = pr.GetValue(convertOnlyOpt);
            vm.DeleteAfterSent.Value = pr.GetValue(deleteAfterConvertOpt);

            var attachments = pr.GetValue(attachmentOpt) ?? [];
            var names = pr.GetValue(attachmentNameOpt) ?? [];
            for (int i = 0; i < attachments.Length; i++)
            {
                string name = i < names.Length
                    ? names[i]
                    : $"{{{{ {vm.ReceiverHeader.Value} }}}}_attachment_{i + 1}.pdf";
                vm.AttachmentPatterns.Add(new AttachmentPattern(attachments[i], name));
            }

            try
            {
                var result = await vm.RunAsync(ct);
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
        });

        var parseResult = root.Parse(args, new ParserConfiguration());
        return await parseResult.InvokeAsync(new InvocationConfiguration());
    }
}
