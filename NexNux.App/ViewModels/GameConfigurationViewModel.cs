using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexNux.Core.Models;
using NexNux.Core.Models.Bgs;
using NexNux.Core.Services;

namespace NexNux.App.ViewModels;

public partial class GameConfigurationViewModel : ViewModelBase
{
    [ObservableProperty] private string _appDataDirectory = "";
    [ObservableProperty] private string _gameDirectory = "";

    private GameService _gameService;
    [ObservableProperty] private int _gameTypeIndex;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _nexNuxDirectory = "";

    public GameConfigurationViewModel(GameService gameService)
    {
        _gameService = gameService;
    }

    [RelayCommand]
    private async Task<Game?> Save()
    {
        return await Task.Run(() =>
        {
            return GameTypeIndex switch
            {
                0 => new Game(Guid.NewGuid(), Name, GameDirectory, NexNuxDirectory),
                1 => new BgsGame(Guid.NewGuid(), Name, GameDirectory, NexNuxDirectory, AppDataDirectory),
                2 => new BgsGamePostSkyrim(Guid.NewGuid(), Name, GameDirectory, NexNuxDirectory, AppDataDirectory),
                _ => null
            };
        });
        // return GameTypeIndex switch
        // {
        //     0 => new Game(Guid.NewGuid(), Name, GameDirectory, NexNuxDirectory),
        //     1 => new BgsGame(Guid.NewGuid(), Name, GameDirectory, NexNuxDirectory, AppDataDirectory),
        //     2 => new BgsGamePostSkyrim(Guid.NewGuid(), Name, GameDirectory, NexNuxDirectory, AppDataDirectory),
        //     _ => null
        // };
    }

    [RelayCommand]
    private void Cancel()
    {
    }

    private bool ValidateForm()
    {
        return true;
    }
}