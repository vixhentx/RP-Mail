using System.Collections.Immutable;
using R3;
using RPMailUI.Contracts.ViewModels;
using RPMailUI.Models;

namespace RPMailUI.Design.ViewModels;

public sealed class DesignErrorViewModel : IErrorViewModel
{
    public IReadOnlyBindableReactiveProperty<ImmutableArray<ErrorItemData>> Items { get; } =
        new BindableReactiveProperty<ImmutableArray<ErrorItemData>>
        ([
            new("The SMTP server rejected one message", "Flyout.Error"),
            new("One attachment could not be found", "Flyout.Warning")
        ]);

    public void Dispose() => Items.Dispose();
}
