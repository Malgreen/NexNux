﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NexNux.Models;
using ReactiveUI;
using Path = System.IO.Path;

namespace NexNux.ViewModels;

public class GameListViewModel : ViewModelBase
{
    public GameListViewModel()
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string gameListFile = Path.Combine(userFolder, "NexNux", "GameList.json");
        MainGameList = new GameList(gameListFile);
        Games = new ObservableCollection<Game>(MainGameList.LoadList());

        ShowConfigDialog = new Interaction<GameConfigViewModel, Game?>();
        ShowRemoveDialog = new Interaction<Game, bool>();
        ShowHomeView = new Interaction<HomeViewModel, bool>();
        
        AddGameCommand = ReactiveCommand.CreateFromTask(AddGame);
        EditGameCommand = ReactiveCommand.CreateFromTask(EditGame);
        ChooseGameCommand = ReactiveCommand.CreateFromTask(ChooseGame);

        RemoveGameCommand = ReactiveCommand.CreateFromTask(RemoveGame);
        Games.CollectionChanged += SaveGameList;        
    }

    public GameList MainGameList { get; set; }

    private ObservableCollection<Game> _games = null!;
    public ObservableCollection<Game> Games
    {
        get => _games; 
        set => this.RaiseAndSetIfChanged(ref _games, value); //Saving to JSON could be done here
    }

    private Game _selectedGame = null!;
    public Game SelectedGame
    {
        get => _selectedGame;
        set => this.RaiseAndSetIfChanged(ref _selectedGame, value);
    }

    public ReactiveCommand<Unit, Unit> AddGameCommand { get; }
    public ReactiveCommand<Unit, Unit> EditGameCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ChooseGameCommand { get; }
    public Interaction<GameConfigViewModel, Game?> ShowConfigDialog { get; }
    public Interaction<Game, bool> ShowRemoveDialog { get; }
    public Interaction<HomeViewModel, bool> ShowHomeView {get;}

    private async Task AddGame()
    {
        try
        {
            GameConfigViewModel config = new GameConfigViewModel();
            Game? result = await ShowConfigDialog.Handle(config);
            if (result == null) return;

            MainGameList.ModifyGame(result.GameName, result.Type, result.DeployDirectory, result.ModsDirectory, result.AppDataDirectory);
            Games = new ObservableCollection<Game>(MainGameList.Games); // There might very well be a better way to do this
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async Task RemoveGame()
    {
        bool result = await ShowRemoveDialog.Handle(_selectedGame); // Will return true if Ok is pressed, false if Cancel is pressed
        if (result)
        {
            MainGameList.RemoveGame(_selectedGame);
            Games = new ObservableCollection<Game>(MainGameList.Games); // as said before, there might be better way to do this
        }
    }

    private async Task EditGame()
    {
        // Here we want to start the config view but pass in the existing stuff :)
        // This probably should not exist, so the button is disabled in the view
        GameConfigViewModel config = new GameConfigViewModel();
        config.GameName = SelectedGame.GameName;
        config.DeployPath = SelectedGame.DeployDirectory;
        config.ModsPath = SelectedGame.ModsDirectory;
        Game? result = await ShowConfigDialog.Handle(config);
        if (result != null)
        {
            Games.Remove(SelectedGame);
            Games.Add(result);
        }
    }

    private async Task ChooseGame()
    {
        HomeViewModel homeViewModel = new HomeViewModel();
        homeViewModel.UpdateGame(SelectedGame);
        await ShowHomeView.Handle(homeViewModel);
    }

    private void SaveGameList(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        //We need to save the list of games that is displayed using a GameList object
        MainGameList.Games = new List<Game>(Games); 
        //For now this is a very simple (and unsafe) way to do this, but in future we could do it with iteration and the ModifyMod method in GameList
        MainGameList.SaveList();
    }

}