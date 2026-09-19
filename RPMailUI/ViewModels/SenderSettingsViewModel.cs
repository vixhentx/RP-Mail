using R3;
using RPMailCore.Models;
using RPMailCore.Serialization;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public sealed class SenderSettingsViewModel : IDisposable
{
	readonly DisposableBag _d = new();


	// 暴露给View
	public ReactiveCommand ImportCommand { get; } = new();
	public ReactiveCommand ExportCommand { get; } = new();
	public BindableReactiveProperty<string> SenderEmail { get; } = new("");
	public BindableReactiveProperty<string> SenderPassword { get; } = new("");
	public BindableReactiveProperty<string> SmtpHost { get; } = new("");

	// 暴露给上级

	public Observable<SenderConfig> ConfOut { get; }

	public SenderSettingsViewModel(
		ConfigService conf,
		JsonFileDialogService fileDialog
	)
	{
		// 配置产生
		ConfOut =
			Observable.CombineLatest(
				SenderEmail,
				SenderPassword,
				SmtpHost,
				static (senderEmail, senderPassword, smtpHost) => new SenderConfig()
				{
					SenderEmail = senderEmail,
					SenderPassword = senderPassword,
					SmtpHost = smtpHost
				}
			);
		var confImport = ImportCommand
			.SelectAwait((_, _) => fileDialog.ReadConf(RPMailJsonContext.Default.SenderConfig))
			.WhereNotNull();

		Observable.Merge(ConfOut, confImport)
			.DistinctUntilChanged()
			.Subscribe(conf.Root, static (module, root) =>
				root.Value = root.Value with { Sender = module }
			)
			.AddTo(ref _d);

		// 配置传入
		conf.Root
			.Select(static x => x.Sender)
			.DistinctUntilChanged()
			.Subscribe(sender =>
			{
				SenderEmail.Value = sender.SenderEmail;
				SenderPassword.Value = sender.SenderPassword;
				SmtpHost.Value = sender.SmtpHost;
			})
			.AddTo(ref _d);

		// 配置导出
		ExportCommand
			.WithLatestFrom(ConfOut, static (_, sender) => sender)
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
