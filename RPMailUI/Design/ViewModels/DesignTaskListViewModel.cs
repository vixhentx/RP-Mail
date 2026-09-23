using System.Collections.Immutable;
using R3;
using RPMailCore.Models;
using RPMailUI.Contracts.ViewModels;
using RPMailUI.Models;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignTaskListViewModel : ITaskListViewModel
{
    public BindableReactiveProperty<string> SearchText { get; } = new();
    public BindableReactiveProperty<string?> SelectedHeader { get; } =
        new("Email");
    public IReadOnlyBindableReactiveProperty<ImmutableArray<string>> AvailableHeaders { get; } =
        new BindableReactiveProperty<ImmutableArray<string>>(["Email", "Name", "Company"]);
    public IReadOnlyBindableReactiveProperty<ImmutableArray<TaskItemData>> Tasks { get; } =
        new BindableReactiveProperty<ImmutableArray<TaskItemData>>
        ([
            new("Alice", MailTaskStatus.Success, "Sent successfully"),
            new("Vix", MailTaskStatus.Running, "Sending..."),
            new("Qlagu", MailTaskStatus.Failed, "The SMTP server rejected the message")
        ]);
    public IReadOnlyBindableReactiveProperty<int> TotalCount { get; } = new BindableReactiveProperty<int>(3);
    public IReadOnlyBindableReactiveProperty<int> SuccessCount { get; } = new BindableReactiveProperty<int>(1);
    public IReadOnlyBindableReactiveProperty<int> FailedCount { get; } = new BindableReactiveProperty<int>(1);

    public void Dispose()
    {
        SearchText.Dispose();
        SelectedHeader.Dispose();
        AvailableHeaders.Dispose();
        Tasks.Dispose();
        TotalCount.Dispose();
        SuccessCount.Dispose();
        FailedCount.Dispose();
    }
}
