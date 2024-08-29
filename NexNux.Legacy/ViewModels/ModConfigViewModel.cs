using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NexNux.Legacy.Models;
using NexNux.Legacy.Utilities;
using ReactiveUI;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace NexNux.Legacy.ViewModels;

public class ModConfigViewModel : ViewModelBase
{
    private IReader? _archiveReader;

    private long _archiveSize;

    private bool _canInstall;

    private Game _currentGame = null!;

    private ModFolderItem _currentRoot = null!;

    private ObservableCollection<IModItem> _extractedFiles = null!;

    private decimal _extractionProgress;

    private bool _isExtracting;

    private string _modArchivePath = null!;

    private string _modName = null!;

    private IModItem _selectedItem = null!;

    private string _statusMessage = null!;

    public ModConfigViewModel()
    {
        ExtractedFiles = new ObservableCollection<IModItem>();
        SetSelectionToRootCommand = ReactiveCommand.Create(SetSelectionToRoot);
        SetSelectionToClipboardCommand = ReactiveCommand.CreateFromTask(SetSelectionToClipboard);
        InstallModCommand = ReactiveCommand.CreateFromTask(InstallMod);
        CancelCommand = ReactiveCommand.CreateFromTask(Cancel);
        ShowErrorDialog = new Interaction<string, bool>();
        SetSelectionToClipboardAsync = new Interaction<string, bool>();

        this.WhenAnyValue(x => x.ModName).Subscribe(_ => ValidateModInput());
    }

    public Game CurrentGame
    {
        get => _currentGame;
        set => this.RaiseAndSetIfChanged(ref _currentGame, value);
    }

    public string ModName
    {
        get => _modName;
        set => this.RaiseAndSetIfChanged(ref _modName, value);
    }

    public string ModArchivePath
    {
        get => _modArchivePath;
        set => this.RaiseAndSetIfChanged(ref _modArchivePath, value);
    }

    public decimal ExtractionProgress
    {
        get => _extractionProgress;
        set => this.RaiseAndSetIfChanged(ref _extractionProgress, value);
    }

    public bool IsExtracting
    {
        get => _isExtracting;
        set => this.RaiseAndSetIfChanged(ref _isExtracting, value);
    }

    public ObservableCollection<IModItem> ExtractedFiles
    {
        get => _extractedFiles;
        set => this.RaiseAndSetIfChanged(ref _extractedFiles, value);
    }

    public IModItem SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public long ArchiveSize
    {
        get => _archiveSize;
        set => this.RaiseAndSetIfChanged(ref _archiveSize, value);
    }

    public ModFolderItem CurrentRoot
    {
        get => _currentRoot;
        set => this.RaiseAndSetIfChanged(ref _currentRoot, value);
    }

    public bool CanInstall
    {
        get => _canInstall;
        set => this.RaiseAndSetIfChanged(ref _canInstall, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Unit> SetSelectionToRootCommand { get; set; }
    public ReactiveCommand<Unit, Unit> SetSelectionToClipboardCommand { get; set; }
    public ReactiveCommand<Unit, Mod?> InstallModCommand { get; }
    public ReactiveCommand<Unit, Mod?> CancelCommand { get; }
    public Interaction<string, bool> ShowErrorDialog { get; }
    public Interaction<string, bool> SetSelectionToClipboardAsync { get; }


    public async void UpdateModArchive(string archivePath)
    {
        // TODO: remove this: if (CurrentGame == null) return;
        ModArchivePath = archivePath;
        ModName = Path.GetFileNameWithoutExtension(archivePath);
        var extractionPath = Path.Combine(CurrentGame.SettingsDirectory, "__installcache");
        await ExtractArchiveAsync(ModArchivePath, extractionPath);
        await UpdateExtractedFiles(extractionPath);
        ValidateModInput();
    }

    private async Task ExtractArchiveAsync(string inputPath, string outputPath)
    {
        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);
        Directory.CreateDirectory(outputPath);
        StatusMessage = "Extracting...";
        IsExtracting = true;
        ExtractionProgress = 0;
        // TODO: Currently extraction progress is just number of entries in the archive, and is updated with 1 each time an entry is extracted
        // TODO: Could be better with bytes, for now it works because of the progressbar Maximum property is being set to the no. of entries

        try
        {
            await using Stream stream = File.OpenRead(inputPath);
            using var
                archive = ArchiveFactory
                    .Open(stream); //We have to use archive->reader, because ReaderFactory does not support Rar archives
            ArchiveSize = archive.Entries.Count(d => !d.IsDirectory); //Used in view
            _archiveReader = archive.ExtractAllEntries();

            while (_archiveReader.MoveToNextEntry())
                await Task.Run(() =>
                {
                    if (!_archiveReader.Entry.IsDirectory)
                    {
                        ExtractionProgress++;
                        StatusMessage = "Extracting " + _archiveReader.Entry;
                        try
                        {
                            _archiveReader.WriteEntryToDirectory(outputPath, new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                        catch (Exception e)
                        {
                            ShowErrorDialog.Handle(e.Message);
                            Debug.WriteLine(e);
                        }
                    }
                });
        }
        catch (Exception e)
        {
            await ShowErrorDialog.Handle(e.Message);
            Debug.WriteLine(e.StackTrace);
        }

        if (ExtractionProgress != 100) ExtractionProgress = 100;
        IsExtracting = false;
    }

    private async Task UpdateExtractedFiles(string rootPath)
    {
        try
        {
            await Task.Run(() =>
            {
                ExtractedFiles = new ObservableCollection<IModItem>();
                var rootItem = new ModFolderItem(rootPath);
                CurrentRoot = rootItem;
                rootItem.ItemName = "root";
                rootItem.SubItems = GetSubItems(rootPath);
                //ExtractedFiles.Add(rootItem);
                ExtractedFiles =
                    rootItem.SubItems; //TBD - should it also show the root folder, or just files within it?
                ValidateModInput();
            });
        }
        catch (Exception e)
        {
            await ShowErrorDialog.Handle(e.Message);
            Debug.WriteLine(e);
        }
    }

    private ObservableCollection<IModItem> GetSubItems(string itemPath)
    {
        var subItems = new ObservableCollection<IModItem>();
        var subFiles = Directory.GetFileSystemEntries(itemPath, "*", SearchOption.TopDirectoryOnly);

        foreach (var subFile in subFiles)
            if (Directory.Exists(subFile)) // If it is a directory
            {
                var currentItem = new ModFolderItem(subFile);
                currentItem.SubItems = GetSubItems(subFile);
                subItems.Add(currentItem);
            }
            else
            {
                var currentItem = new ModFileItem(subFile);
                subItems.Add(currentItem);
            }

        return subItems;
    }

    /// <summary>
    ///     Searches through a given IModItem's SubItems, and each of their SubItems, for a IModItem by name.
    ///     Also checks the given IModItem if it matches the name.
    /// </summary>
    /// <param name="modItem">The item to search through</param>
    /// <param name="itemName">The name of  the item to search for</param>
    /// <returns>Whether an item by the given name exists in SubItems/given IModItem.</returns>
    private bool ExistsInSubItems(IModItem modItem, string itemName)
    {
        if (modItem is null) return false;
        if (modItem.ItemName == itemName || modItem.SubItems.Any(item =>
                string.Equals(item.ItemName, itemName, StringComparison.CurrentCultureIgnoreCase))) return true;
        foreach (var subItem in modItem.SubItems)
            if (ExistsInSubItems(subItem, itemName))
                return true;

        return false;
    }

    private bool ExistsInRoot(string itemName)
    {
        if (CurrentRoot is null) return false;
        if (CurrentRoot.SubItems.Any(item =>
                string.Equals(item.ItemName, itemName, StringComparison.CurrentCultureIgnoreCase))) return true;
        return false;
    }

    private bool ExistsInRootByFileExtension(string fileExtension)
    {
        if (CurrentRoot is null) return false;
        if (CurrentRoot.SubItems.Any(item => string.Equals(Path.GetExtension(item.ItemName), fileExtension,
                StringComparison.CurrentCultureIgnoreCase))) return true;
        return false;
    }

    private async void SetSelectionToRoot()
    {
        if (SelectedItem is ModFolderItem) await UpdateExtractedFiles(SelectedItem.ItemPath);
    }

    private async Task SetSelectionToClipboard()
    {
        try
        {
            var selectedPath = Path.GetDirectoryName(SelectedItem.ItemPath) ?? throw new InvalidOperationException();
            await SetSelectionToClipboardAsync.Handle(selectedPath);
        }
        catch (Exception e)
        {
            await ShowErrorDialog.Handle(e.Message);
            Debug.WriteLine(e.StackTrace);
        }
    }

    public async Task<Mod?> InstallMod()
    {
        try
        {
            Mod mod = null!;
            await Task.Run(() =>
            {
                var installedModPath = Path.Combine(CurrentGame.ModsDirectory, ModName);
                mod = new Mod(ModName, installedModPath, 0, CurrentGame.GetAllMods().Count,
                    false); //FileSize is updated in the ModListVM
            });
            return mod;
        }
        catch (Exception e)
        {
            await ShowErrorDialog.Handle(e.Message);
            Debug.WriteLine(e.StackTrace);
            return null;
        }
    }

    public Task<Mod?> Cancel()
    {
        _archiveReader?.Cancel();
        return Task.FromResult<Mod?>(null); //There is null-checking in the modlistviewmodel
    }

    private bool IsGamebryoContentAtRoot()
    {
        if (ExistsInSubItems(CurrentRoot, "data")) return false;
        if (ExistsInRoot("textures") || ExistsInRoot("meshes") || ExistsInRoot("sound") ||
            ExistsInRoot("menus") || ExistsInRoot("music") || ExistsInRoot("shaders")) return true;
        if (ExistsInRootByFileExtension(".esm") || ExistsInRootByFileExtension(".esl") ||
            ExistsInRootByFileExtension(".esp") || ExistsInRootByFileExtension(".ba2") ||
            ExistsInRootByFileExtension(".bsa")) return true;
        return false;
    }

    private void ValidateModInput()
    {
        CanInstall = false;
        if (string.IsNullOrWhiteSpace(ModName))
        {
            StatusMessage = "❌ Mod must have a name";
        }
        else if (ModName.StartsWith("."))
        {
            StatusMessage = "❌ Mod name cannot start with \'.\' ";
        }
        else if (ModName.StartsWith(" "))
        {
            StatusMessage = "❌ Mod name cannot start with whitespace";
        }
        else if (ModName.Length > 50)
        {
            StatusMessage = "❌ Mod name is too long";
        }
        else if (CurrentGame.Type is not GameType.Generic && !IsGamebryoContentAtRoot())
        {
            StatusMessage = "⚠️ No game content at root";
            CanInstall = true;
        }
        else
        {
            StatusMessage = "✔️ Looks good";
            CanInstall = true;
        }
    }
}