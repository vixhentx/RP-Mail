using System.Collections.Immutable;

namespace RPMailCore.Models;

[AttributeUsage(AttributeTargets.Field)]
public sealed class TaskStatusMetaAttribute(int ordinal) : Attribute
{
    public int Ordinal { get; } = ordinal;
}

public enum MailTaskStatus
{
    [TaskStatusMeta(2)]
    Ready,

    [TaskStatusMeta(1)]
    Pending,

    [TaskStatusMeta(0)]
    Running,

    [TaskStatusMeta(3)]
    Success,

    [TaskStatusMeta(4)]
    Failed,
}

public readonly record struct TaskStateChanged(int Index, MailTaskStatus Status, string? Message);

public sealed record FailedRow(ImmutableDictionary<string, string> Row, string Reason);

public sealed record ContentParsed
{
    public required int RowIndex { get; init; }
    public required string Receiver { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public required ImmutableArray<string> Attachments { get; init; }
    public required string HtmlPath { get; init; }
    public required string OutputDir { get; init; }
    public required ImmutableDictionary<string, string> RawRow { get; init; }
}

public sealed class SenderConfig
{
    public required string SmtpHost { get; set; }
    public required string SenderEmail { get; set; }
    public required string SenderPassword { get; set; }
}

public sealed class TemplateConfig
{
    public required string CsvPath { get; set; }
    public required string HtmlPath { get; set; }
    public required string Subject { get; set; }
    public string ReceiverHeader { get; set; } = "Receiver";
    public string CharSet { get; set; } = "utf-8";
}

public sealed class AttachmentPattern
{
    public required string Source { get; set; }
    public required string Name { get; set; }
}

public sealed class OutputConfig
{
    public string OutputDir { get; set; } = "Output";
    public bool SaveHtmlFile { get; set; }
    public bool SaveRawDocs { get; set; }
    public bool ConvertOnly { get; set; }
    public bool DeleteAfterSent { get; set; }
    public ImmutableArray<AttachmentPattern> Attachments { get; set; } = [];
}

public sealed class MailConfig
{
    public SenderConfig? Sender { get; set; }
    public required TemplateConfig Template { get; set; }
    public OutputConfig Output { get; set; } = new();

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
            HtmlPath = "samples/template.html",
            Subject = "{{ Title }}",
        },
        Output = new()
        {
            OutputDir = "Output",
            Attachments =
            [
                new() { Source = "samples/attachment.typ", Name = "{{ Name }}_attachment_1.pdf" },
            ],
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
