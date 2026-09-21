using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPMailUI.Contracts.ViewModels;
using RPMailUI.Resources;
using RPMailUI.Services;
using RPMailUI.ViewModels;
using RPMailUI.Views;

namespace RPMailUI;

public partial class App : Application
{
	readonly RPMailServiceProvider rootSp = new();
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ResourcesEntry.Load();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
			var scope = rootSp.CreateScope();

			var persistence = scope.GetService<PersistenceService>();
			var window = scope.GetService<MainWindow>();
			var vm = scope.GetService<IMainWindowViewModel>();

			window.DataContext = vm;
            window.Closed += (_, _) => scope.Dispose();
            desktop.MainWindow = window;

			persistence.Load();

        }

        base.OnFrameworkInitializationCompleted();
    }
}
