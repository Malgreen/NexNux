using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexNux.App.Utilities;
using NexNux.Core.Models;
using NexNux.Core.Services;

namespace NexNux.App.ViewModels;

public partial class GameSelectionViewModel : ViewModelBase
{
    private readonly GameService _gameService = new();
    [ObservableProperty] private IEnumerable<Game> _games = new List<Game>();
    [ObservableProperty] private Game? _selectedGame;

    public GameSelectionViewModel()
    {
        GetGames();
    }

    [RelayCommand]
    private void AddGame()
    {
        Console.WriteLine("adding game");
    }

    [RelayCommand]
    private void RemoveGame()
    {
        Console.WriteLine("removing game");
    }

    [RelayCommand]
    private void ChooseGame()
    {
        if (SelectedGame == null) return;
        Console.WriteLine($"choosing game: {SelectedGame.Name}");
    }

    private async void GetGames()
    {
        Games = await TaskHelper.TryRunAsync(_gameService.GetAll) ?? new List<Game>();
        // try
        // {
        //     Games = await _gameService.GetAll();
        // }
        // catch (Exception ex)
        // {
        //     
        // }
    }
}