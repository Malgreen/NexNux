using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NexNux.Legacy.ViewModels;
using ReactiveUI;

namespace NexNux.Legacy.Views;

public partial class GameConfigView : ReactiveWindow<GameConfigViewModel>
{
    public GameConfigView()
    {
        InitializeComponent();
        //this.WhenActivated(b => b(ViewModel!.CancelCommand.Subscribe(Close)));
        this.WhenActivated(d => d(ViewModel!.SaveGameCommand.Subscribe(Close)));
        this.WhenActivated(d => d(ViewModel!.ShowErrorDialog.RegisterHandler(DoShowErrorDialogAsync)));
        this.WhenActivated(d => d(ViewModel!.ShowDeployFolderDialog.RegisterHandler(DoShowDeployFolderDialog)));
        this.WhenActivated(d => d(ViewModel!.ShowModsFolderDialog.RegisterHandler(DoShowModsFolderDialog)));
        this.WhenActivated(d => d(ViewModel!.ShowAppDataFolderDialog.RegisterHandler(DoShowAppDataFolderDialog)));
    }

    private async Task DoShowErrorDialogAsync(InteractionContext<string, bool> interactionContext)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Error!", interactionContext.Input, ButtonEnum.Ok,
            MsBox.Avalonia.Enums.Icon.Warning);
        await messageBox.ShowAsPopupAsync(this);
        interactionContext.SetOutput(true);
    }

    private async Task DoShowDeployFolderDialog(InteractionContext<Unit, string> interactionContext)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose Deploy Folder",
            AllowMultiple = false,
            SuggestedStartLocation = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop)
        });

        if (folders.Count >= 1)
            interactionContext.SetOutput(folders[0].TryGetLocalPath() ?? string.Empty);
        else
            interactionContext.SetOutput(string.Empty);
    }

    private async Task DoShowModsFolderDialog(InteractionContext<Unit, string> interactionContext)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose Mods Folder",
            AllowMultiple = false,
            SuggestedStartLocation = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop)
        });

        if (folders.Count >= 1)
            interactionContext.SetOutput(folders[0].TryGetLocalPath() ?? string.Empty);
        else
            interactionContext.SetOutput(string.Empty);
    }

    private async Task DoShowAppDataFolderDialog(InteractionContext<Unit, string> interactionContext)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose AppData Folder",
            AllowMultiple = false,
            SuggestedStartLocation = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop)
        });

        if (folders.Count >= 1)
            interactionContext.SetOutput(folders[0].TryGetLocalPath() ?? string.Empty);
        else
            interactionContext.SetOutput(string.Empty);
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        // I think that handling this any other way would be over-engineering, as this does not really need to be tested
        Debug.WriteLine("Game Config cancelled, sender: " + sender + ", Args:" + e);
        Close();
    }
}