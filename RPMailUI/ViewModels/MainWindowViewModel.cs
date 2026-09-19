using R3;

namespace RPMailUI.ViewModels;

public class MainWindowViewModel : IDisposable
{

    readonly DisposableBag _d = new();
	public ContentSettingsViewModel Content { get; }
    public SenderSettingsViewModel Sender { get; }
    public ConvertSettingsViewModel Convert { get; }
    public MailRunViewModel Run { get; }
	public ErrorViewModel Error { get; }

	public MainWindowViewModel(
		ContentSettingsViewModel contentVm,
		ConvertSettingsViewModel convertVm,
		SenderSettingsViewModel senderVm,
		MailRunViewModel runVm,
		ErrorViewModel errorVm
	) 
	{
		Content = contentVm;
		Convert = convertVm;
		Sender = senderVm;
		Run = runVm;
		Error = errorVm;
    }


    public void Dispose()
    {
        _d.Dispose();
		Content.Dispose();
		Convert.Dispose();
		Sender.Dispose();
		Run.Dispose();
    }
}
