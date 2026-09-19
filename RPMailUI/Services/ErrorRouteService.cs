
using System.Collections.Immutable;
using R3;
using RPMailUI.Models;

namespace RPMailUI.Services;

public class ErrorRouteService : IDisposable
{
	public const int MaxErrorCount = 5;
	public Subject<Unit> Reset { get; } = new();
	public Subject<ErrorItemData> Error { get; } = new();
	public Observable<ImmutableArray<ErrorItemData>> ErrorList { get; }

	public ErrorRouteService()
	{
		ErrorList =
			Observable.Merge(
				Reset.Select(static r => (reset: true, error: default(ErrorItemData)!)),
				Error.Select(static e => (reset: false, error: e))
			)
			.Scan(
				ImmutableArray<ErrorItemData>.Empty,
				static (acc, e) => (acc,e) switch
				{
					{ e.reset: true } => [],
					{ acc.Length: < MaxErrorCount } => [ ..acc, e.error ],
					{ acc.Length: >= MaxErrorCount } => [ ..acc[^(MaxErrorCount-1)..], e.error]
				}
			);
	}

	public void Dispose()
	{
		Reset.Dispose();
		Error.Dispose();
	}
}