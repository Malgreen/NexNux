using System;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NexNux.Legacy.ViewModels;
using ReactiveUI;

namespace NexNux.Legacy.Views;

public partial class ModConfigView : ReactiveWindow<ModConfigViewModel>
{
    public ModConfigView()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.InstallModCommand.Subscribe(Close)));
        this.WhenActivated(d => d(ViewModel!.CancelCommand.Subscribe(Close)));
        this.WhenActivated(d => d(ViewModel!.ShowErrorDialog.RegisterHandler(DoShowErrorDialogAsync)));
        this.WhenActivated(d =>
            d(ViewModel!.SetSelectionToClipboardAsync.RegisterHandler(DoSetSelectionToClipboardAsync)));
    }

    private async Task DoShowErrorDialogAsync(InteractionContext<string, bool> interactionContext)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Error!", interactionContext.Input, ButtonEnum.Ok,
            MsBox.Avalonia.Enums.Icon.Warning);
        await messageBox.ShowAsPopupAsync(this);
        interactionContext.SetOutput(true);
    }

    private async Task DoSetSelectionToClipboardAsync(InteractionContext<string, bool> interactionContext)
    {
        await Clipboard!.SetTextAsync(interactionContext.Input);
        interactionContext.SetOutput(true);
    }
}