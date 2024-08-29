using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NexNux.Legacy.ViewModels;
using ReactiveUI;

namespace NexNux.Legacy.Views;

public partial class HomeView : ReactiveWindow<HomeViewModel>
{
    public HomeView()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.ShowErrorDialog.RegisterHandler(DoShowErrorDialogAsync)));
    }

    private async Task DoShowErrorDialogAsync(InteractionContext<string, bool> interactionContext)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Error!", interactionContext.Input, ButtonEnum.Ok,
            MsBox.Avalonia.Enums.Icon.Warning);
        await messageBox.ShowAsPopupAsync(this);
        interactionContext.SetOutput(true);
    }
}