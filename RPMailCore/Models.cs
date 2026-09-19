using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace RPMailCore.Models;

public enum MailTaskStatus
{
    Ready,
    Pending,
    Running,
    Success,
    Failed,
}

public static class MailTaskStatusExtensions
{
    extension(MailTaskStatus status)
    {
        public int Ordinal => status switch
		{
			MailTaskStatus.Ready => 2,
			MailTaskStatus.Pending => 1,
			MailTaskStatus.Running => 0,
			MailTaskStatus.Success => 3,
			MailTaskStatus.Failed => 4,
			_ => throw new ArgumentException($"Status {status} is invalid")
		};
        public string Key => $"TaskStatus.{status}";
    }
}

public interface IMailRunOutput
{
    string Text { get; }
    LogLevel Level { get; }
    Exception? Exception { get; }
}

public abstract record MailRunOutput : IMailRunOutput
{
    public abstract string Text { get; }
    public virtual LogLevel Level => LogLevel.Information;
    public virtual Exception? Exception => null;
}

public sealed record RowsLoadedOutput(
    IReadOnlyList<ImmutableDictionary<string, string>> Rows) : MailRunOutput
{
    public override string Text => "";
}

public sealed record TaskStateOutput(
    int Index,
    MailTaskStatus Status,
    string? Message) : MailRunOutput
{
    public override string Text => Message ?? "";
}

public sealed record ProgressOutput(double Progress) : MailRunOutput
{
    public override string Text => "";
}

public sealed record MessageOutput(
    string Message,
    LogLevel logLevel = LogLevel.Information,
    Exception? Exception = null) : MailRunOutput
{
    public override string Text => Message;
    public override LogLevel Level => logLevel;
    public override Exception? Exception { get; } = Exception;
}

public sealed record RunCompletedOutput(RunResult Result) : MailRunOutput
{
    public override string Text => "";
}

public sealed record FailedRow(ImmutableDictionary<string, string> Row, string Reason);

public sealed record ContentParsed
{
    public required int RowIndex { get; init; }
    public required string Email { get; init; }
    public required string Subject { get; init; }
    public required string BodyHtml { get; init; }
    public required ImmutableArray<string> Attachments { get; init; }
    public required string BodyHtmlPath { get; init; }
    public required string OutputDir { get; init; }
    public required ImmutableDictionary<string, string> UserAttributes { get; init; }
    public required ImmutableDictionary<string, string> ExtraAttributes { get; init; }
}

public sealed record SenderConfig
{
    public required string SmtpHost { get; init; }
    public required string SenderEmail { get; init; }
    public required string SenderPassword { get; init; }
}

public sealed record TemplateConfig
{
    public required string CsvPath { get; init; }
    public required string BodyHtmlPath { get; init; }
    public required string Subject { get; init; }
    public string CharSet { get; init; } = "utf-8";
    public ImmutableDictionary<string, string> ExtraAttributes { get; init; } = [];
    public ImmutableArray<AttachmentPattern> Attachments { get; init; } = [];
}

public sealed record AttachmentPattern
{
    public required string Source { get; init; }
    public required string Name { get; init; }
}

public sealed record OutputConfig
{
    public required string OutputDir { get; init; }
    public required bool SaveHtmlFile { get; init; }
    public required bool SaveRawDocs { get; init; }
    public required bool ConvertOnly { get; init; }
    public required bool DeleteAfterSent { get; init; }
}

public sealed record MailConfig
{
    public required SenderConfig Sender { get; init; }
    public required TemplateConfig Template { get; init; }
    public required OutputConfig Output { get; init; }

    public static MailConfig CreateTemplate() => new()
    {
        Sender = new()
        {
            SmtpHost = "smtp.example.com:465",
            SenderEmail = "sender@example.com",
            SenderPassword = "your-password",
        },
        Template = new()
        {
            CsvPath = "samples/sample.csv",
            BodyHtmlPath = "samples/template.html",
            Subject = "{{ user.title }}",
            ExtraAttributes = [ new("company", "RobotPilots") ],
            Attachments =
            [
                new() { Source = "samples/attachment.typ", Name = "{{ user.name }}_attachment_1.pdf" },
            ],
        },
        Output = new()
        {
            OutputDir = "Output",
			SaveHtmlFile = false,
			ConvertOnly = false,
			DeleteAfterSent = false,
			SaveRawDocs = true
        },
    };
}

public sealed record RunResult(
    bool Success,
    int TotalRows,
    int FailedRows,
    int SentCount,
    string RealOutputDir,
    string? FailedCsvPath,
    Exception? FatalException);
