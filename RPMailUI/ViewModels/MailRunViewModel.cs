using System;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using RPMailCore.Coordination;
using RPMailCore.Models;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class MailRunViewModel : IDisposable
{
    private const int MaxErrorCount = 5;

    private DisposableBag _d = new();

    public MailRunCoordinator Coordinator { get; }
    public TaskListViewModel TaskList { get; }

    public ObservableList<ErrorItemData> Errors { get; } = [];

    public BindableReactiveProperty<string> ConsoleLog { get; } = new("");
    public BindableReactiveProperty<double> Progress { get; } = new(0);
    public BindableReactiveProperty<bool> ShouldRetry { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldOpenOutputFolder { get; } = new(false);

    public ReactiveCommand OpenOutputFolderCommand { get; } = new();
    public ReactiveCommand RetryCommand { get; } = new();

    // 直接引用 Coordinator.StartCommand，只需转发其可绑定命令语义，避免创建第二个运行入口。
    public ReactiveCommand<Unit, RunResult> StartCommand { get; }

    public MailRunViewModel(
        BindableReactiveProperty<PersistedSettings> configuration,
        Action<string> setRetryCsvPath,
        Observable<string> moduleErrors)
    {
        Coordinator = new MailRunCoordinator();
        TaskList = new TaskListViewModel(Coordinator.Output);
        StartCommand = Coordinator.StartCommand;

        OpenOutputFolderCommand.Subscribe(_ =>
        {
            if (Coordinator.LastResult is { } result)
                PathOpenHelper.OpenDirectory(result.RealOutputDir);
        }).AddTo(ref _d);

        RetryCommand.Subscribe(_ =>
        {
            if (Coordinator.LastResult?.FailedCsvPath is { } failedCsv)
                setRetryCsvPath(failedCsv);
        }).AddTo(ref _d);

        moduleErrors
            .ObserveOnUIThreadDispatcher()
            .Subscribe(message => AddError(LogLevel.Error, message))
            .AddTo(ref _d);

        // 这里是 UI 配置到 Core 的唯一副作用边界。
        configuration.AsObservable()
            .ObserveOnUIThreadDispatcher()
            .Subscribe(ApplyConfigurationToCoordinator)
            .AddTo(ref _d);

        Coordinator.Output
            .ObserveOnUIThreadDispatcher()
            .Subscribe(output =>
            {
                switch (output)
                {
                    case ProgressOutput progress:
                        Progress.Value = progress.Progress;
                        break;
                    case MessageOutput message when message.Level >= LogLevel.Warning:
                        AddError(message.Level, message.Text);
                        break;
                    case RunCompletedOutput completed:
                        ShouldOpenOutputFolder.Value = completed.Result.Success;
                        ShouldRetry.Value = completed.Result.Success && completed.Result.FailedRows > 0;
                        break;
                }

                // 仅 MessageOutput 的文本追加到控制台；其他结构化输出不追加。
                if (output is MessageOutput messageOutput && !string.IsNullOrEmpty(messageOutput.Text))
                    ConsoleLog.Value += messageOutput.Text + Environment.NewLine;
            }).AddTo(ref _d);

        Coordinator.IsRunning.AsObservable()
            .ObserveOnUIThreadDispatcher()
            .Where(running => running)
            .Subscribe(_ =>
            {
                Errors.Clear();
                Progress.Value = 0;
            }).AddTo(ref _d);

        Coordinator.IsRunning.AsObservable()
            .ObserveOnUIThreadDispatcher()
            .Where(running => !running)
            .Skip(1)
            .Subscribe(_ =>
            {
                var result = Coordinator.LastResult;
                ShouldOpenOutputFolder.Value = result?.Success == true;
                ShouldRetry.Value = result is { Success: true, FailedRows: > 0 };
            }).AddTo(ref _d);

        OpenOutputFolderCommand.AddTo(ref _d);
        RetryCommand.AddTo(ref _d);
        ConsoleLog.AddTo(ref _d);
        Progress.AddTo(ref _d);
        ShouldRetry.AddTo(ref _d);
        ShouldOpenOutputFolder.AddTo(ref _d);
    }

    private void ApplyConfigurationToCoordinator(PersistedSettings settings)
    {
        if (settings is null)
            return;

        Coordinator.CsvPath.Value = settings.CsvFile;
        Coordinator.BodyHtmlPath.Value = settings.BodyHtmlPath;
        Coordinator.Subject.Value = settings.Subject;
        Coordinator.CharSet.Value = settings.CharSet;
        Coordinator.SmtpHost.Value = settings.SmtpHost;
        Coordinator.SenderEmail.Value = settings.SenderEmail;
        Coordinator.SenderPassword.Value = settings.SenderPassword;
        Coordinator.OutputDir.Value = settings.OutputFolder;
        Coordinator.DeleteAfterSent.Value = settings.IsDeleteAfterSent;
        Coordinator.ConvertOnly.Value = settings.IsConvertOnly;
        Coordinator.SaveRawDocs.Value = settings.IsSaveRawDoc;
        Coordinator.SaveHtmlFile.Value = settings.IsSaveHtml;

        Coordinator.AttachmentPatterns.Clear();
        foreach (var pattern in settings.Attachments)
        {
            Coordinator.AttachmentPatterns.Add(new() { Source = pattern.SourceText, Name = pattern.DestinationText });
        }

        Coordinator.ExtraAttributes.Clear();
        foreach (var attribute in settings.ExtraAttributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.Key))
                continue;
            Coordinator.ExtraAttributes[attribute.Key] = attribute.Value;
        }
    }

    public void AddError(LogLevel level, string message)
    {
        string key = level >= LogLevel.Error ? "Flyout.Error" : "Flyout.Warning";
        Errors.Add(new ErrorItemData(message, key));
        if (Errors.Count > MaxErrorCount)
            Errors.RemoveAt(0);
    }

    public void Dispose()
    {
        _d.Dispose();
        TaskList.Dispose();
        Coordinator.Dispose();
    }
}
