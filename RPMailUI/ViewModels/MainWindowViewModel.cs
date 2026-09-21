using R3;
using RPMailUI.Contracts.ViewModels;

namespace RPMailUI.ViewModels;

public class MainWindowViewModel : IMainWindowViewModel
{

	readonly DisposableBag _d = new();
	public IWorkspaceConfigViewModel Workspace { get; }
	public IContentSettingsViewModel Content { get; }
    public ISenderSettingsViewModel Sender { get; }
    public IConvertSettingsViewModel Convert { get; }
    public IMailRunViewModel Run { get; }
	public IErrorViewModel Error { get; }

	public MainWindowViewModel(
		IWorkspaceConfigViewModel workspaceVm,
		IContentSettingsViewModel contentVm,
		IConvertSettingsViewModel convertVm,
		ISenderSettingsViewModel senderVm,
		IMailRunViewModel runVm,
		IErrorViewModel errorVm
	) 
	{
		Workspace = workspaceVm;
		Content = contentVm;
		Convert = convertVm;
		Sender = senderVm;
		Run = runVm;
		Error = errorVm;
    }


    public void Dispose()
	{
		_d.Dispose();
		Workspace.Dispose();
		Content.Dispose();
		Convert.Dispose();
		Sender.Dispose();
		Run.Dispose();
    }
}
