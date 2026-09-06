using System.Text.Json.Serialization;

namespace RPMailUI.Services;

public sealed record PersistedAttachment(string SourceText, string DestinationText);

public sealed record PersistedExtraAttribute(string Key, string Value);

public sealed record PersistedSettings
{
    public string CsvFile { get; init; } = "";
    public string BodyHtmlPath { get; init; } = "";
    public string Subject { get; init; } = "";
    public string CharSet { get; init; } = "utf-8";
    public List<PersistedAttachment> Attachments { get; init; } = [];
    public List<PersistedExtraAttribute> ExtraAttributes { get; init; } = [];
    public string SenderEmail { get; init; } = "";
    public string SenderPassword { get; init; } = "";
    public string SmtpHost { get; init; } = "";
    public string OutputFolder { get; init; } = "Output";
    public bool IsDeleteAfterSent { get; init; }
    public bool IsConvertOnly { get; init; }
    public bool IsSaveRawDoc { get; init; }
    public bool IsSaveHtml { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PersistedSettings))]
public partial class PersistedSettingsContext : JsonSerializerContext;
