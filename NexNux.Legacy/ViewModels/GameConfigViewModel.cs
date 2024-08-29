using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NexNux.Legacy.Models;
using ReactiveUI;

namespace NexNux.Legacy.ViewModels;

public class GameConfigViewModel : ViewModelBase
{
    private string _appDataPath = null!;

    private bool _canAddGame;

    private string _deployPath = null!;

    private string _gameName = null!;

    private GameType _gameType;

    private string _modsPath = null!;

    private string _statusMessage = null!;

    private int _typeIndex;

    public GameConfigViewModel()
    {
        SaveGameCommand = ReactiveCommand.CreateFromTask(SaveGame);
        ChooseDeployPathCommand = ReactiveCommand.CreateFromTask(ChooseDeployPath);
        ChooseModsPathCommand = ReactiveCommand.CreateFromTask(ChooseModsPath);
        ChooseAppDataPathCommand = ReactiveCommand.CreateFromTask(ChooseAppDataPath);
        ShowErrorDialog = new Interaction<string, bool>();
        ShowDeployFolderDialog = new Interaction<Unit, string>();
        ShowModsFolderDialog = new Interaction<Unit, string>();
        ShowAppDataFolderDialog = new Interaction<Unit, string>();

        CanAddGame = false;
        StatusMessage = string.Empty;
        GameName = string.Empty;
        GameType = GameType.Generic;
        TypeIndex = 0;
        ModsPath = string.Empty;
        DeployPath = string.Empty;

        this.WhenAnyValue(x => x.TypeIndex).Subscribe(_ => SetGameType());
        this.WhenAnyValue(x => x.GameName).Subscribe(_ => ValidateGameInput());
        this.WhenAnyValue(x => x.DeployPath).Subscribe(_ => ValidateGameInput());
        this.WhenAnyValue(x => x.ModsPath).Subscribe(_ => ValidateGameInput());
        this.WhenAnyValue(x => x.AppDataPath).Subscribe(_ => ValidateGameInput());
        this.WhenAnyValue(x => x.GameType).Subscribe(_ => ValidateGameInput());
    }

    public string GameName
    {
        get => _gameName;
        set => this.RaiseAndSetIfChanged(ref _gameName, value);
    }

    public GameType GameType
    {
        get => _gameType;
        set => this.RaiseAndSetIfChanged(ref _gameType, value);
    }

    public int TypeIndex
    {
        get => _typeIndex;
        set => this.RaiseAndSetIfChanged(ref _typeIndex, value);
    }

    public string DeployPath
    {
        get => _deployPath;
        set => this.RaiseAndSetIfChanged(ref _deployPath, value);
    }

    public string ModsPath
    {
        get => _modsPath;
        set => this.RaiseAndSetIfChanged(ref _modsPath, value);
    }

    public string AppDataPath
    {
        get => _appDataPath;
        set => this.RaiseAndSetIfChanged(ref _appDataPath, value);
    }

    public bool CanAddGame
    {
        get => _canAddGame;
        set => this.RaiseAndSetIfChanged(ref _canAddGame, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Game?> SaveGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ChooseDeployPathCommand { get; }
    public ReactiveCommand<Unit, Unit> ChooseModsPathCommand { get; }
    public ReactiveCommand<Unit, Unit> ChooseAppDataPathCommand { get; }
    public Interaction<string, bool> ShowErrorDialog { get; }
    public Interaction<Unit, string> ShowDeployFolderDialog { get; }
    public Interaction<Unit, string> ShowModsFolderDialog { get; }
    public Interaction<Unit, string> ShowAppDataFolderDialog { get; }

    private void SetGameType()
    {
        switch (TypeIndex)
        {
            case 0:
                GameType = GameType.Generic;
                break;
            case 1:
                GameType = GameType.BGS;
                break;
            case 2:
                GameType = GameType.BGSPostSkyrim;
                break;
        }
    }

    public async Task<Game?> SaveGame()
    {
        try
        {
            var game = new Game(GameName, GameType, DeployPath, ModsPath, AppDataPath);
            return game;
        }
        catch (Exception e)
        {
            await ShowErrorDialog.Handle(e.Message);
            Debug.WriteLine(e);
            return null; // We check for null returns when opening the game config window
        }
    }

    private async Task ChooseDeployPath()
    {
        DeployPath = await ShowDeployFolderDialog.Handle(Unit.Default);
    }

    private async Task ChooseModsPath()
    {
        ModsPath = await ShowModsFolderDialog.Handle(Unit.Default);
    }

    private async Task ChooseAppDataPath()
    {
        AppDataPath = await ShowAppDataFolderDialog.Handle(Unit.Default);
    }

    private void ValidateGameInput()
    {
        CanAddGame = false;
        if (string.IsNullOrWhiteSpace(GameName))
        {
            StatusMessage = "❌ Game must have a name";
        }
        else if (GameName.StartsWith(" "))
        {
            StatusMessage = "❌ Game name cannot start with whitespace";
        }
        else if (GameName.StartsWith("."))
        {
            StatusMessage = "❌ Game name cannot start with \'.\'";
        }
        else if (string.IsNullOrWhiteSpace(DeployPath))
        {
            StatusMessage = "❌ Game must have a deploy directory";
        }
        else if (string.IsNullOrWhiteSpace(ModsPath))
        {
            StatusMessage = "❌ Game must have a mods directory";
        }
        else if (!Directory.Exists(DeployPath))
        {
            StatusMessage = "❌ Deploy directory does not exist";
        }
        else if (!Directory.Exists(ModsPath))
        {
            StatusMessage = "❌ Mods directory does not exist";
        }
        else if (!Equals(Path.GetPathRoot(DeployPath), Path.GetPathRoot(ModsPath)))
        {
            StatusMessage = "❌ Directories must reside on the same drive";
        }
        else if
            (Directory.EnumerateFileSystemEntries(ModsPath)
             .Any()) // This makes it so the editing a game no longer works
        {
            StatusMessage = "❌ Mods directory must be empty";
        }
        else if (GameType != GameType.Generic && !Directory.Exists(AppDataPath))
        {
            StatusMessage = "❌ AppData directory does not exist";
        }
        else
        {
            StatusMessage = "✅ Looks good";
            CanAddGame = true;
        }
    }
}