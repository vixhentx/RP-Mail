using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPMailUI.Resources;
using RPMailUI.ViewModels;
using RPMailUI.Views;

namespace RPMailUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ResourcesEntry.Load();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
			var sp = new RPMailServiceProvider();
			var window = sp.GetService<MainWindow>();
			var vm = sp.GetService<MainWindowViewModel>();

			window.DataContext = vm;
            window.Closed += (_, _) => vm.Dispose();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
