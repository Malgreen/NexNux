using CommunityToolkit.Mvvm.ComponentModel;

namespace NexNux.Core.Models.Bgs;

public partial class BgsGame : Game
{
    [ObservableProperty] private string _appDataDirectory;

    public BgsGame(Guid id, string name, string gameDirectory, string nexNuxDirectory, string appDataDirectory) : base(
        id, name, gameDirectory, nexNuxDirectory)
    {
        AppDataDirectory = appDataDirectory;
    }
}