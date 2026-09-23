using System.Collections.Immutable;
using R3;
using RPMailUI.Models;
using RPMailUI.Services;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.ViewModels;

public class ErrorViewModel : IErrorViewModel
{
	public IReadOnlyBindableReactiveProperty<ImmutableArray<ErrorItemData>> Items { get; }
	public IReadOnlyBindableReactiveProperty<bool> HasErrors { get; }

	public ErrorViewModel(
		ErrorRouteService es
	)
	{
		Items = es.ErrorList
			.ToReadOnlyBindableReactiveProperty([]);
		HasErrors = Items.AsObservable()
			.Select(static items => !items.IsEmpty)
			.ToReadOnlyBindableReactiveProperty(false);
	}

	public void Dispose()
	{
		Items.Dispose();
		HasErrors.Dispose();
	}
}
