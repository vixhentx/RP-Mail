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

public sealed record SenderConfig(string SmtpHost, string SenderEmail, string SenderPassword);

public sealed record TemplateConfig(string CsvPath, string HtmlPath, string Subject, string ReceiverHeader, string CharSet);

public sealed record AttachmentPattern(string Source, string Name);

public sealed record OutputConfig(
    string OutputDir,
    bool SaveHtmlFile,
    bool SaveRawDocs,
    bool ConvertOnly,
    bool DeleteAfterSent,
    ImmutableArray<AttachmentPattern> Attachments);

public sealed record MailConfig(SenderConfig Sender, TemplateConfig Template, OutputConfig Output);

public sealed record RunResult(
    bool Success,
    int TotalRows,
    int FailedRows,
    int SentCount,
    string RealOutputDir,
    string? FailedCsvPath,
    Exception? FatalException);
