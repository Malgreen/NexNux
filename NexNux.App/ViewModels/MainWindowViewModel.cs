using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using NexNux.App.Views;

namespace NexNux.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";

    [RelayCommand]
    public void ShowSelectionScreen()
    {
        var x = new GameSelectionWindow
        {
            DataContext = new GameSelectionViewModel()
        };

        if (Application.Current.ApplicationLifetime is ClassicDesktopStyleApplicationLifetime desktop)
        {
            var y = desktop.MainWindow;
            Console.WriteLine(y);
        }
    }
}