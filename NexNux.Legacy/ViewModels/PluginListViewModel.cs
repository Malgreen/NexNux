using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using NexNux.Legacy.Models;
using NexNux.Legacy.Models.Gamebryo;
using ReactiveUI;

namespace NexNux.Legacy.ViewModels;

public class PluginListViewModel : ViewModelBase
{
    private bool _busy;

    private string _busyMessage = null!;

    private Game? _currentGame;

    private GamebryoPluginList _currentPluginList = null!;

    private GamebryoPlugin _selectedPlugin = null!;

    private ObservableCollection<GamebryoPlugin> _visiblePlugins = null!;

    public PluginListViewModel()
    {
        Busy = false;
        BusyMessage = "";
        ShowErrorDialog = new Interaction<string, bool>();
        this.WhenAnyValue(x => x.CurrentGame).Subscribe(_ => UpdateCurrentGame());
    }

    public Game? CurrentGame
    {
        get => _currentGame;
        set => this.RaiseAndSetIfChanged(ref _currentGame, value);
    }

    public GamebryoPluginList CurrentPluginList
    {
        get => _currentPluginList;
        set => this.RaiseAndSetIfChanged(ref _currentPluginList, value);
    }

    public ObservableCollection<GamebryoPlugin> VisiblePlugins
    {
        get => _visiblePlugins;
        set => this.RaiseAndSetIfChanged(ref _visiblePlugins, value);
    }

    public GamebryoPlugin SelectedPlugin
    {
        get => _selectedPlugin;
        set => this.RaiseAndSetIfChanged(ref _selectedPlugin, value);
    }

    public bool Busy
    {
        get => _busy;
        set => this.RaiseAndSetIfChanged(ref _busy, value);
    }

    public string BusyMessage
    {
        get => _busyMessage;
        set => this.RaiseAndSetIfChanged(ref _busyMessage, value);
    }

    public Interaction<string, bool> ShowErrorDialog { get; }
    public event EventHandler<EventArgs>? PluginListChanged;

    public void UpdateCurrentGame()
    {
        if (CurrentGame == null || CurrentGame.AppDataDirectory == null) return;
        CurrentPluginList = new GamebryoPluginList(
            CurrentGame.DeployDirectory,
            CurrentGame.SettingsDirectory,
            CurrentGame.AppDataDirectory,
            CurrentGame.Type
        );
        UpdatePlugins(CurrentPluginList);
    }

    public void UpdatePlugins(GamebryoPluginList pluginList)
    {
        var prevPlugins = VisiblePlugins;
        CurrentPluginList = pluginList;
        VisiblePlugins = CurrentPluginList.Plugins;
        SetPluginListeners(VisiblePlugins, prevPlugins);
    }

    public async void ReorderPlugin(int oldIndex, int newIndex)
    {
        Busy = true;
        VisiblePlugins.Move(oldIndex, newIndex);
        CurrentPluginList.Plugins = VisiblePlugins;
        await Task.Run(() => CurrentPluginList.RefreshFromDeployDirectory());
        // PluginListChanged?.Invoke(this, e);
        Busy = false;
    }

    private void SetPluginListeners(IList? newItems, IList? oldItems)
    {
        if (newItems != null)
            foreach (INotifyPropertyChanged plugin in newItems)
                plugin.PropertyChanged += Plugin_PropertyChanged;
        if (oldItems != null)
            foreach (INotifyPropertyChanged plugin in oldItems)
                plugin.PropertyChanged -= Plugin_PropertyChanged;
    }

    private async void Plugin_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // This should only handled the enabled status, as it should only be run once to avoid writing the file 
        // at the same time
        if (e.PropertyName != "Enabled") return;
        BusyMessage = "Saving...";
        Busy = true;
        CurrentPluginList.Plugins = VisiblePlugins;
        await Task.Run(() => CurrentPluginList.RefreshFromDeployDirectory());
        PluginListChanged?.Invoke(this, e);
        Busy = false;
    }
}