using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace NexNux.App.Utilities;

public static class DialogHelper
{
    public static void ShowDialog(string title, string message)
    {
        var dialogWindow = new Window
        {
            Title = title,
            Content = new TextBlock { Text = message }
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        if (desktop.MainWindow != null) dialogWindow.ShowDialog(desktop.MainWindow);
    }
}