using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Material.Icons;
using NexNux.Legacy.Models;
using NexNux.Legacy.Models.Gamebryo;
using NexNux.Legacy.Utilities;
using NexNux.Legacy.Utilities.ModDeployment;
using NexNux.Legacy.Views;
using ReactiveUI;

namespace NexNux.Legacy.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private Game? _currentGame;

    private ModList _currentModList = null!;

    private GamebryoPluginList _currentPluginList = null!;

    private string _currentTitle = null!;

    private double _deploymentProgress;

    private string _deploymentStatus = null!;

    private MaterialIconKind _deploymentStatusIcon;

    private double _deploymentTotal;

    private bool _isDeployed;

    private bool _isDeploying;

    private ModListViewModel? _modListViewModel;
    private PluginListViewModel? _pluginListViewModel;

    private ObservableCollection<NexNuxTabItem> _tabItems = null!;

    public HomeViewModel()
    {
        DeploymentProgress = 0;
        DeploymentTotal = 1;

        ShowErrorDialog = new Interaction<string, bool>();
        DeployModsCommand = ReactiveCommand.Create(DeployMods);
        ClearModsCommand = ReactiveCommand.Create(ClearMods);

        this.WhenAnyValue(x => x.IsDeployed).Subscribe(_ => UpdateDeploymentStatus());
        this.WhenAnyValue(x => x.IsDeploying).Subscribe(_ => UpdateDeploymentStatus());
    }

    public Game? CurrentGame
    {
        get => _currentGame;
        set => this.RaiseAndSetIfChanged(ref _currentGame, value);
    }

    public string CurrentTitle
    {
        get => _currentTitle;
        set => this.RaiseAndSetIfChanged(ref _currentTitle, value);
    }

    public ObservableCollection<NexNuxTabItem> TabItems
    {
        get => _tabItems;
        set => this.RaiseAndSetIfChanged(ref _tabItems, value);
    }

    public ModList CurrentModList
    {
        get => _currentModList;
        set => this.RaiseAndSetIfChanged(ref _currentModList, value);
    }

    public GamebryoPluginList CurrentPluginList
    {
        get => _currentPluginList;
        set => this.RaiseAndSetIfChanged(ref _currentPluginList, value);
    }

    public bool IsDeployed
    {
        get => _isDeployed;
        set => this.RaiseAndSetIfChanged(ref _isDeployed, value);
    }

    public bool IsDeploying
    {
        get => _isDeploying;
        set => this.RaiseAndSetIfChanged(ref _isDeploying, value);
    }

    public string DeploymentStatus
    {
        get => _deploymentStatus;
        set => this.RaiseAndSetIfChanged(ref _deploymentStatus, value);
    }

    public MaterialIconKind DeploymentStatusIcon
    {
        get => _deploymentStatusIcon;
        set => this.RaiseAndSetIfChanged(ref _deploymentStatusIcon, value);
    }

    public double DeploymentProgress
    {
        get => _deploymentProgress;
        set => this.RaiseAndSetIfChanged(ref _deploymentProgress, value);
    }

    public double DeploymentTotal
    {
        get => _deploymentTotal;
        set => this.RaiseAndSetIfChanged(ref _deploymentTotal, value);
    }

    public ReactiveCommand<Unit, Unit> DeployModsCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearModsCommand { get; }
    public Interaction<string, bool> ShowErrorDialog { get; }

    public void UpdateGame(Game game)
    {
        CurrentGame = game;
        CurrentTitle = "NexNux - " + game;
        TabItems = new ObservableCollection<NexNuxTabItem>();
        IsDeployed = CurrentGame.Settings.RecentlyDeployed;
        InitializeTabs();
    }

    public void InitializeTabs()
    {
        InitializeModsTab();
        if (CurrentGame?.Type != GameType.Generic) InitializePluginsTab();
        InitializeSettingsTab();
    }

    private void InitializeModsTab()
    {
        _modListViewModel = new ModListViewModel
        {
            CurrentGame = CurrentGame
        };
        _modListViewModel.ModListChanged += ModListViewModel_OnModListChanged;
        CurrentModList = _modListViewModel.CurrentModList;
        var modListView = new ModListView
        {
            DataContext = _modListViewModel
        };

        var modsTabItem = new NexNuxTabItem("Mods", MaterialIconKind.Plugin, modListView);
        TabItems.Add(modsTabItem);
    }

    private void ModListViewModel_OnModListChanged(object? sender, EventArgs e)
    {
        IsDeployed = false;
        if (sender is ModListViewModel mlvm) CurrentModList = mlvm.CurrentModList;
    }

    private void InitializePluginsTab()
    {
        if (CurrentGame == null || CurrentGame.AppDataDirectory == null) return;
        _pluginListViewModel = new PluginListViewModel
        {
            CurrentGame = CurrentGame
        };
        CurrentPluginList = _pluginListViewModel.CurrentPluginList;
        var pluginListView = new PluginListView
        {
            DataContext = _pluginListViewModel
        };

        var pluginsTabItem = new NexNuxTabItem("Plugins", MaterialIconKind.FormatListBulleted, pluginListView);
        TabItems.Add(pluginsTabItem);
    }

    private void InitializeSettingsTab()
    {
        var settingsTabItem = new NexNuxTabItem("Settings", MaterialIconKind.Settings, new UserControl());
        TabItems.Add(settingsTabItem);
    }

    private async void DeployMods()
    {
        if (CurrentGame == null) return;
        try
        {
            IsDeploying = true;
            DeploymentTotal = GetFileAmount(CurrentModList.GetActiveMods());

            IModDeployer modDeployer = new SymLinkDeployer(CurrentGame);
            modDeployer.FileDeployed += ModDeployer_FileDeployed;
            await Task.Run(() => modDeployer.Deploy(CurrentModList.GetActiveMods()));

            if (CurrentGame.Type != GameType.Generic && _pluginListViewModel != null)
            {
                await Task.Run(() => CurrentPluginList.RefreshFromDeployDirectory());
                _pluginListViewModel.UpdatePlugins(CurrentPluginList);
            }

            IsDeploying = false;
            IsDeployed = true;
            DeploymentProgress = 0;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.StackTrace);
            IsDeployed = false;
            await ShowErrorDialog.Handle(e.Message);
        }
    }

    private void ModDeployer_FileDeployed(object? sender, FileDeployedArgs e)
    {
        DeploymentProgress = e.Progress;
    }

    private static double GetFileAmount(List<Mod?> mods)
    {
        var amount = 0;
        foreach (var mod in mods)
        {
            if (mod == null) continue;
            var dir = new DirectoryInfo(mod.ModPath);
            foreach (var _ in dir.GetFiles("*", SearchOption.AllDirectories)) amount++;
        }

        return amount;
    }

    private async void ClearMods()
    {
        if (CurrentGame == null) return;
        try
        {
            IsDeploying = true;

            IModDeployer modDeployer = new SymLinkDeployer(CurrentGame);
            await Task.Run(() => modDeployer.Clear());

            if (CurrentGame.Type != GameType.Generic)
                await Task.Run(() => CurrentPluginList.RefreshFromDeployDirectory());

            IsDeployed = false;
            IsDeploying = false;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.StackTrace);
            await ShowErrorDialog.Handle(e.Message);
        }
    }

    private void UpdateDeploymentStatus()
    {
        if (IsDeploying)
        {
            DeploymentStatusIcon = MaterialIconKind.HourglassEmpty;
            DeploymentStatus = "Deploying...";
        }
        else if (IsDeployed)
        {
            DeploymentStatusIcon = MaterialIconKind.Check;
            DeploymentStatus = "Mods deployed";
        }
        else
        {
            DeploymentStatusIcon = MaterialIconKind.Warning;
            DeploymentStatus = "Deployment needed";
        }

        if (CurrentGame == null) return;
        CurrentGame.Settings.RecentlyDeployed = IsDeployed;
        CurrentGame.Settings.Save();
        if (_pluginListViewModel != null)
        {
            _pluginListViewModel.BusyMessage = "Please deploy before managing plugins.";
            _pluginListViewModel.Busy = !IsDeployed;
        }
    }
}