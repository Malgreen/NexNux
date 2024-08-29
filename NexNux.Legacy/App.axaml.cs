using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NexNux.Legacy.ViewModels;
using NexNux.Legacy.Views;

namespace NexNux.Legacy;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new GameListView
            {
                DataContext = new GameListViewModel()
            };

        base.OnFrameworkInitializationCompleted();
    }
}