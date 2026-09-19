using System.Collections.Immutable;
using R3;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public class ErrorViewModel : IDisposable
{
	public IReadOnlyBindableReactiveProperty<ImmutableArray<ErrorItemData>> Items { get; }

	public ErrorViewModel(
		ErrorRouteService es
	)
	{
		Items = es.ErrorList
			.ToReadOnlyBindableReactiveProperty();
	}

	public void Dispose() => Items.Dispose();
}