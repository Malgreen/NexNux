using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NexNux.Legacy.Models;
using NexNux.Legacy.ViewModels;
using ReactiveUI;

namespace NexNux.Legacy.Views;

public partial class GameListView : ReactiveWindow<GameListViewModel>
{
    public GameListView()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.ShowConfigDialog.RegisterHandler(DoShowGameConfigDialogAsync)));
        this.WhenActivated(d => d(ViewModel!.ShowRemoveDialog.RegisterHandler(DoShowGameRemoveDialogAsync)));
        this.WhenActivated(d => d(ViewModel!.ShowHomeView.RegisterHandler(DoShowHomeView)));
    }

    private async Task DoShowGameConfigDialogAsync(InteractionContext<GameConfigViewModel, Game?> interactionContext)
    {
        var dialog = new GameConfigView
        {
            DataContext = interactionContext.Input
        };

        var result = await dialog.ShowDialog<Game>(this);
        interactionContext.SetOutput(result);
    }

    private async Task DoShowGameRemoveDialogAsync(InteractionContext<Game, bool> interactionContext)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(
            "Are you sure?",
            $"This will also delete ALL mods and settings previously configured for {interactionContext.Input} within NexNux",
            ButtonEnum.OkCancel,
            MsBox.Avalonia.Enums.Icon.Warning
        );
        var result = await messageBox.ShowAsPopupAsync(this);
        interactionContext.SetOutput(result == ButtonResult.Ok);
    }

    private Task DoShowHomeView(InteractionContext<HomeViewModel, bool> interactionContext)
    {
        var homeView = new HomeView();
        homeView.DataContext = interactionContext.Input;
        homeView.Show();
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            desktopLifetime.MainWindow = homeView;

        interactionContext.SetOutput(true); // maybe this should not be an interaction
        Close();
        return Task.CompletedTask;
    }
}