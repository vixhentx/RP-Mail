using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class SenderSettingsViewModel : IDisposable
{
	readonly DisposableBag _d = new();
	bool _synching = false;

	// 暴露给View
	public ReactiveCommand ImportCommand { get; } = new();
	public ReactiveCommand ExportCommand { get; } = new();
	public BindableReactiveProperty<string> SenderEmail { get; } = new("");
	public BindableReactiveProperty<string> SenderPassword { get; } = new("");
	public BindableReactiveProperty<string> SmtpHost { get; } = new("");

	public SenderSettingsViewModel(
		ConfigService conf,
		JsonFileDialogService fileDialog
	)
	{
		// 配置产生
		var confOut =
			Observable.CombineLatest(
				SenderEmail,
				SenderPassword,
				SmtpHost,
				static (senderEmail, senderPassword, smtpHost) =>
					new SenderConfig()
					{
						SenderEmail = senderEmail,
						SenderPassword = senderPassword,
						SmtpHost = smtpHost
					}

			)
			.Where(_ => !_synching);

		var confImport = ImportCommand
			.SelectAwait((_, _) => fileDialog.ReadConf(RPMailJsonContext.Default.SenderConfig))
			.WhereNotNull();

		Observable.Merge(confOut, confImport)
			.DistinctUntilChanged()
			.Subscribe(conf.Root, static (m, root) => root.Value = root.Value with
				{
					Sender = m
				})
			.AddTo(ref _d);

		// 配置传入
		conf.Root
			.Where(_ => !_synching)
			.Select(static x => x.Sender)
			.DistinctUntilChanged()
			.Subscribe(sender =>
			{
				_synching = true;

				SenderEmail.Value = sender.SenderEmail;
				SenderPassword.Value = sender.SenderPassword;
				SmtpHost.Value = sender.SmtpHost;

				_synching = false;
			})
			.AddTo(ref _d);

		// 配置导出
		ExportCommand
			.WithLatestFrom(conf.Root, static (_, conf) => conf.Sender)
			.SubscribeAwait((sender, _) => fileDialog.WriteConf(sender, RPMailJsonContext.Default.SenderConfig, "sender_module.json"))
			.AddTo(ref _d);
	}


	public void Dispose()
	{
		_d.Dispose();
		ImportCommand.Dispose();
		ExportCommand.Dispose();
		SenderEmail.Dispose();
		SenderPassword.Dispose();
		SmtpHost.Dispose();
	}
}
