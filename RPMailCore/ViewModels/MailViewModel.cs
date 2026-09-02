using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using R3;
using RPMailCore.Models;
using RPMailCore.Processors;

namespace RPMailCore.ViewModels;

public class MailViewModel : IDisposable
{
    private readonly MailRunProcessor _processor;

    public BindableReactiveProperty<string> CsvPath { get; } = new("");
    public BindableReactiveProperty<string> HtmlPath { get; } = new("");
    public BindableReactiveProperty<string> Subject { get; } = new("");
    public BindableReactiveProperty<string> ReceiverHeader { get; } = new("Receiver");
    public BindableReactiveProperty<string> CharSet { get; } = new("utf-8");
    public BindableReactiveProperty<string> SmtpHost { get; } = new("");
    public BindableReactiveProperty<string> SenderEmail { get; } = new("");
    public BindableReactiveProperty<string> SenderPassword { get; } = new("");
    public BindableReactiveProperty<string> OutputDir { get; } = new("Output");
    public BindableReactiveProperty<bool> SaveHtmlFile { get; } = new(false);
    public BindableReactiveProperty<bool> SaveRawDocs { get; } = new(false);
    public BindableReactiveProperty<bool> ConvertOnly { get; } = new(false);
    public BindableReactiveProperty<bool> DeleteAfterSent { get; } = new(false);

    public List<AttachmentPattern> AttachmentPatterns { get; } = [];

    public BindableReactiveProperty<bool> IsRunning { get; } = new(false);

    public ReactiveCommand<Unit, RunResult> StartCommand { get; }

    public RunResult? LastResult { get; private set; }

    public Observable<IReadOnlyList<ImmutableDictionary<string, string>>> RowsLoaded => _processor.RowsLoaded;
    public Observable<TaskStateChanged> TaskStateChanged => _processor.TaskStateChanged;
    public Observable<double> ProgressChanged => _processor.ProgressChanged;

    public MailViewModel(ILogger? logger = null)
    {
        _processor = new MailRunProcessor(logger);
        StartCommand = new ReactiveCommand<Unit, RunResult>(
            IsRunning.AsObservable().Select(x => !x),
            true,
            async (_, ct) => await RunAsync(BuildConfig(), ct));
    }

    public async Task<RunResult> RunAsync(MailConfig config, CancellationToken ct = default)
    {
        if (IsRunning.Value)
            throw new InvalidOperationException("Mail run is already in progress.");
        IsRunning.Value = true;
        try
        {
            LastResult = await Task.Run(() => _processor.RunAsync(config, ct), ct);
            return LastResult;
        }
        finally
        {
            IsRunning.Value = false;
        }
    }

    private MailConfig BuildConfig() => new()
    {
        Sender = new()
        {
            SmtpHost = SmtpHost.Value,
            SenderEmail = SenderEmail.Value,
            SenderPassword = SenderPassword.Value,
        },
        Template = new()
        {
            CsvPath = CsvPath.Value,
            HtmlPath = HtmlPath.Value,
            Subject = Subject.Value,
            ReceiverHeader = ReceiverHeader.Value,
            CharSet = CharSet.Value,
        },
        Output = new()
        {
            OutputDir = OutputDir.Value,
            SaveHtmlFile = SaveHtmlFile.Value,
            SaveRawDocs = SaveRawDocs.Value,
            ConvertOnly = ConvertOnly.Value,
            DeleteAfterSent = DeleteAfterSent.Value,
            Attachments = AttachmentPatterns.ToImmutableArray(),
        },
    };

    public void Dispose()
    {
        StartCommand.Dispose();
        IsRunning.Dispose();
        _processor.Dispose();
    }
}
