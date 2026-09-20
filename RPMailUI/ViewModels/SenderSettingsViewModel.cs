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
		// 配置传入
		conf.Pipe
			.DistinctUntilChangedBy(static p => p.Value.Sender)
			.Where(static p => p.Source is ConfChangingSource.Import)
			.Select(static p => p.Value.Sender)
			.Subscribe(sender =>
			{
				_synching = true;

				SenderEmail.Value = sender.SenderEmail;
				SenderPassword.Value = sender.SenderPassword;
				SmtpHost.Value = sender.SmtpHost;

				_synching = false;
			})
			.AddTo(ref _d);

		// 用户编辑产生配置
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
			.Where(_ => !_synching)
			.WithLatestFrom(
				conf.Pipe,
				static (sender, pipe) => new ConfPipe(
					pipe.Value with
					{
						Sender = sender
					},
					ConfChangingSource.UserEdit
				)
			)
			.Where(static p => p.Source is ConfChangingSource.UserEdit)
			.Subscribe(conf.Pipe, static (module,conf) => conf.Value = module)
			.AddTo(ref _d);

		// 配置导入
		ImportCommand
			.SelectAwait((_, _) => fileDialog.ReadConf(RPMailJsonContext.Default.SenderConfig))
			.WhereNotNull()
			.WithLatestFrom(
				conf.Pipe,
				static (sender, pipe) => new ConfPipe(
					pipe.Value with
					{
						Sender = sender
					},
					ConfChangingSource.Import
				)
			)
			.Where(static p => p.Source is ConfChangingSource.Import)
			.Subscribe(conf.Pipe, static (module,conf) => conf.Value = module)
			.AddTo(ref _d);

		// 配置导出
		ExportCommand
			.WithLatestFrom(conf.Pipe, static (_, pipe) => pipe.Value.Sender)
			.SubscribeAwait((sender, _) =>
				fileDialog.WriteConf(
					sender,
					RPMailJsonContext.Default.SenderConfig,
					"sender_module.json"
				)
			)
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
