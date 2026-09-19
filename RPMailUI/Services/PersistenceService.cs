using System.Text.Json;
using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;

namespace RPMailUI.Services;

/// <summary>
/// 持久化服务. 用来处理自动保存, 以及启动时加载
/// </summary>
public class PersistenceService : IDisposable
{
    public static string DefautlSettingsPath => Path.Combine(AppContext.BaseDirectory, "RPMailUI-Persisted.json");
	public const float DebounceMs = 500;
	readonly DisposableBag _d = new();
	readonly Subject<MailConfig> _confOut = new();
	public Observable<MailConfig> ConfOut => _confOut;
	public PersistenceService(
		Observable<MailConfig> ConfIn
	)
	{
		ConfIn
			.Debounce(TimeSpan.FromMilliseconds(DebounceMs))
			.SubscribeAwait((conf, ct) => SaveAsync(conf, ct: ct))
			.AddTo(ref _d);
	}

	public async ValueTask LoadAsync(string path = default!, CancellationToken ct = default)
	{
		path ??= DefautlSettingsPath;

		// fall back 为 默认模板
		MailConfig conf = MailConfig.CreateTemplate();
		if(File.Exists(path)) try
		{
			var text = await File.ReadAllTextAsync(path,ct);
			conf =
				JsonSerializer.Deserialize(
					text,
					RPMailJsonContext.Default.MailConfig
				)
				is {} c ?
				c : conf;
		}
		catch {}

		_confOut.OnNext(conf);
	}

	public async ValueTask SaveAsync(MailConfig conf, string path = default!, CancellationToken ct = default)
	{
		path ??= DefautlSettingsPath;
		
		string text =
			JsonSerializer.Serialize(
				conf,
				RPMailJsonContext.Default.MailConfig
			);

		var dir = Path.GetDirectoryName(path);
		if(dir is {} d && !Directory.Exists(dir))
			Directory.CreateDirectory(d);

		await File.WriteAllTextAsync(path, text, ct);
	}

	public void Dispose()
	{
		_d.Dispose();
		_confOut.Dispose();
	}
}