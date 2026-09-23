using System;
using System.Collections.Immutable;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Contracts.ViewModels;

public interface IErrorViewModel : IDisposable
{
    IReadOnlyBindableReactiveProperty<ImmutableArray<ErrorItemData>> Items { get; }
    IReadOnlyBindableReactiveProperty<bool> HasErrors { get; }
}
