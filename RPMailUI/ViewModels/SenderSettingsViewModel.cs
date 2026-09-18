using System;
using System.Text.Json;
using System.Threading.Tasks;
using R3;
using RPMailCore.Models;
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

	public SenderSettingsViewModel()
	{
		ConfOut =
			Observable.CombineLatest(
				SenderEmail,
				SenderPassword,
				SmtpHost,
				static ( senderEmail, senderPassword, smtpHost) => new SenderConfig()
				{
					SenderEmail = senderEmail,
					SenderPassword = senderPassword,
					SmtpHost = smtpHost
				}
			);
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
