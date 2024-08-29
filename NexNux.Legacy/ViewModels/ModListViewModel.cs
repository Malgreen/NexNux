using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NexNux.Legacy.Models;
using ReactiveUI;

namespace NexNux.Legacy.ViewModels;

public class ModListViewModel : ViewModelBase
{
    private Game? _currentGame;

    private ModList _currentModList = null!;

    private string _modInfo = null!;

    private Mod? _selectedMod;

    private ObservableCollection<Mod?> _visibleMods = null!;

    public ModListViewModel()
    {
        ShowModInstallDialog = new Interaction<ModConfigViewModel, Mod?>();
        ShowModUninstallDialog = new Interaction<Mod, bool>();
        ShowErrorDialog = new Interaction<string, bool>();
        ShowModExistsDialog = new Interaction<Mod?, bool>();

        VisibleMods = new ObservableCollection<Mod?>();
        VisibleMods.CollectionChanged += UpdateModList;

        InstallModCommand = ReactiveCommand.Create(InstallMod);
        UninstallModCommand = ReactiveCommand.Create(UninstallMod);
        this.WhenAnyValue(x => x.CurrentGame).Subscribe(_ => UpdateCurrentGame());
        this.WhenAnyValue(x => x.SelectedMod).Subscribe(_ => UpdateModInfo());
    }

    public Game? CurrentGame
    {
        get => _currentGame;
        set => this.RaiseAndSetIfChanged(ref _currentGame, value);
    }

    public ModList CurrentModList
    {
        get => _currentModList;
        set => this.RaiseAndSetIfChanged(ref _currentModList, value);
    }

    public ObservableCollection<Mod?> VisibleMods
    {
        get => _visibleMods;
        set => this.RaiseAndSetIfChanged(ref _visibleMods, value);
    }

    public Mod? SelectedMod
    {
        get => _selectedMod;
        set => this.RaiseAndSetIfChanged(ref _selectedMod, value);
    }

    public string ModInfo
    {
        get => _modInfo;
        set => this.RaiseAndSetIfChanged(ref _modInfo, value);
    }

    public ReactiveCommand<Unit, Unit> InstallModCommand { get; }
    public ReactiveCommand<Unit, Unit> UninstallModCommand { get; }
    public Interaction<ModConfigViewModel, Mod?> ShowModInstallDialog { get; }
    public Interaction<Mod, bool> ShowModUninstallDialog { get; }
    public Interaction<string, bool> ShowErrorDialog { get; }
    public Interaction<Mod?, bool> ShowModExistsDialog { get; }
    public event EventHandler<EventArgs>? ModListChanged;


    public void UpdateCurrentGame()
    {
        if (CurrentGame == null) return;
        VisibleMods.CollectionChanged -= UpdateModList; //Not doing this might lead to memory leak
        CurrentModList = new ModList(CurrentGame.SettingsDirectory);
        var previousVisibleMods = VisibleMods;
        VisibleMods = new ObservableCollection<Mod?>(CurrentModList.LoadList());

        SetModListeners(VisibleMods, previousVisibleMods);

        VisibleMods.CollectionChanged += UpdateModList;
    }

    private async void InstallMod()
    {
        try
        {
            if (CurrentGame == null) return;
            var installCacheDir = Path.Combine(CurrentGame.SettingsDirectory, "__installcache");
            var modConfigViewModel = new ModConfigViewModel
            {
                CurrentGame = CurrentGame
            };
            var mod = await ShowModInstallDialog.Handle(modConfigViewModel);
            if (mod == null)
            {
                if (Directory.Exists(installCacheDir) && !modConfigViewModel.IsExtracting)
                    Directory.Delete(installCacheDir, true);
                return;
            }

            var existingMod = VisibleMods.FirstOrDefault(item => item?.ModName == mod.ModName);
            var installedModPath = Path.Combine(CurrentGame.ModsDirectory, mod.ModName);

            if (existingMod != null)
            {
                var result = await ShowModExistsDialog.Handle(mod);
                if (result)
                {
                    MoveExtractedFiles(modConfigViewModel.CurrentRoot.ItemPath, installedModPath);
                    var dirInfo = new DirectoryInfo(existingMod.ModPath);
                    existingMod.FileSize = Math.Round(await Task.Run(() =>
                                                          dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                                              .Sum(file => file.Length)) *
                                                      0.000001); //converts bytes to mb
                }
            }
            else
            {
                MoveExtractedFiles(modConfigViewModel.CurrentRoot.ItemPath, installedModPath);
                var dirInfo = new DirectoryInfo(mod.ModPath);
                mod.FileSize = Math.Round(await Task.Run(() =>
                                              dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                                  .Sum(file => file.Length)) *
                                          0.000001); //converts bytes to mb

                VisibleMods.Add(mod);
            }

            if (Directory.Exists(installCacheDir) && !modConfigViewModel.IsExtracting)
                Directory.Delete(installCacheDir, true);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.StackTrace);
            await ShowErrorDialog.Handle(e.Message);
        }
    }

    private async void UninstallMod()
    {
        if (SelectedMod == null) return;
        try
        {
            var result = await ShowModUninstallDialog.Handle(SelectedMod);
            if (!result) return;
            SelectedMod.DeleteFiles();
            VisibleMods.Remove(SelectedMod);
        }
        catch (DirectoryNotFoundException)
        {
            VisibleMods.Remove(SelectedMod); //this exception doesn't matter
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.StackTrace);
            await ShowErrorDialog.Handle(e.Message);
        }
    }

    private void UpdateModInfo()
    {
        if (SelectedMod == null)
        {
            ModInfo = "No mod selected";
        }
        else
        {
            var finalModInfo = string.Empty;
            finalModInfo += SelectedMod.ModName + "\n";
            finalModInfo += SelectedMod.ModPath + "\n";
            finalModInfo += SelectedMod.FileSize + " MB\n";
            finalModInfo += "Is enabled: " + SelectedMod.Enabled + "\n";
            //This can all be changed later, but the subscription to property changes works
            ModInfo = finalModInfo;
        }
    }

    private void UpdateModList(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var mod in VisibleMods)
        {
            if (mod == null) continue;
            mod.Index = VisibleMods.IndexOf(mod);
        }

        SetModListeners(e.NewItems, e.OldItems);
        SaveVisibleList();
        ModListChanged?.Invoke(this, e);
    }

    private void SetModListeners(IList? newItems, IList? oldItems)
    {
        if (newItems != null)
            foreach (INotifyPropertyChanged mod in newItems)
                mod.PropertyChanged += Mod_PropertyChanged;
        if (oldItems != null)
            foreach (INotifyPropertyChanged mod in oldItems)
                mod.PropertyChanged -= Mod_PropertyChanged;
    }

    private void Mod_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateModInfo();
        SaveVisibleList();
        ModListChanged?.Invoke(this, e);
    }

    private void SaveVisibleList()
    {
        CurrentModList.Mods = VisibleMods.ToList();
        CurrentModList.SaveList();
    }

    private void MoveExtractedFiles(string source, string target)
    {
        Directory.CreateDirectory(target); // Without this, there is an exception when installing empty files
        // Taken from https://stackoverflow.com/a/2553245

        var sourcePath = source;
        var targetPath = target;
        var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
            .GroupBy(s => Path.GetDirectoryName(s));
        foreach (var folder in files)
        {
            var targetFolder = folder.Key?.Replace(sourcePath, targetPath);
            if (targetFolder == null) continue;
            Directory.CreateDirectory(targetFolder);
            foreach (var file in folder)
            {
                var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                if (File.Exists(targetFile)) File.Delete(targetFile);
                File.Move(file, targetFile);
            }
        }
    }
}