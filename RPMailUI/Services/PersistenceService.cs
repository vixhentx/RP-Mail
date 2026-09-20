using System.Text.Json;
using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;
using RPMailUI.Models;

namespace RPMailUI.Services;

/// <summary>
/// 持久化服务. 用来处理自动保存, 以及启动时加载
/// </summary>
public class PersistenceService : IDisposable
{
	public static string DefautlSettingsPath =>
		Path.Combine(AppContext.BaseDirectory, "RPMailUI-Persisted.json");

	public const float DebounceMs = 500;

	readonly DisposableBag _d = new();
	readonly ConfigService _conf;

	public PersistenceService(
		ConfigService conf
	)
	{
		_conf = conf;

		conf.Pipe
			.Select(static p => p.Value)
			.Debounce(TimeSpan.FromMilliseconds(DebounceMs))
			.SubscribeAwait((conf, ct) => SaveAsync(conf, ct: ct))
			.AddTo(ref _d);
	}

	public void Load(string path = default!)
	{
		path ??= DefautlSettingsPath;

		// fall back 为默认模板
		MailConfig conf = MailConfig.CreateTemplate();
		if (File.Exists(path))
		{
			try
			{
				var text = File.ReadAllText(path);
				conf =
					JsonSerializer.Deserialize(
						text,
						RPMailJsonContext.Default.MailConfig
					)
					is {} c
						? c
						: conf;
			}
			catch
			{
			}
		}

		_conf.Pipe.Value = new(
			Value: conf,
			Source: ConfChangingSource.Import
		);
	}

	public async ValueTask SaveAsync(
		MailConfig conf,
		string path = default!,
		CancellationToken ct = default
	)
	{
		path ??= DefautlSettingsPath;

		string text =
			JsonSerializer.Serialize(
				conf,
				RPMailJsonContext.Default.MailConfig
			);

		var dir = Path.GetDirectoryName(path);
		if (dir is {} d && !Directory.Exists(dir))
			Directory.CreateDirectory(d);

		await File.WriteAllTextAsync(path, text, ct);
	}

	public void Dispose()
	{
		_d.Dispose();
	}
}
