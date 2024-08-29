using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using NexNux.Legacy.Models;

namespace NexNux.Legacy.Utilities.ModDeployment;

public sealed class SymLinkDeployer : IModDeployer
{
    private readonly string _cachePath;
    private readonly string _deployPath;
    private readonly string _jsonPath;
    private readonly List<string> _cachedFiles;
    private List<string> _deployedFiles;

    public SymLinkDeployer(Game game)
    {
        CurrentGame = game;
        _deployPath = CurrentGame.DeployDirectory;
        _cachePath = Path.Combine(CurrentGame.SettingsDirectory, "__deploycache");
        _jsonPath = Path.Combine(CurrentGame.SettingsDirectory, "DeployedFiles.json");
        _deployedFiles = new List<string>();
        _cachedFiles = new List<string>();

        Directory.CreateDirectory(_cachePath);
    }

    public event EventHandler<FileDeployedArgs>? FileDeployed;
    public Game CurrentGame { get; }

    /// <summary>
    ///     Deploys given list of files to the deployer's game's 'deploy' folder.
    ///     If the current platform is Windows, this will be done using HardLinks, on other platforms it will use SymLinks.
    /// </summary>
    /// <param name="mods"></param>
    public Task Deploy(List<Mod?> mods)
    {
        LoadLinkedMods();
        RestoreCache();
        LinkMods(mods);
        SaveLinkedMods();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Purges all deployed files from the deployer's game's 'deploy' folder
    /// </summary>
    public Task Clear()
    {
        LoadLinkedMods();
        RestoreCache();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Creates a hard link using Windows DLL import, therefore this method only works on Windows.
    ///     Taken from https://stackoverflow.com/a/3387777
    /// </summary>
    /// <param name="lpFileName">Target path</param>
    /// <param name="lpExistingFileName">Source path</param>
    /// <param name="lpSecurityAttributes">Should be IntPtr.Zero</param>
    /// <returns></returns>
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
    );

    /// <summary>
    ///     Removes all links in the DeployedFiles.json, and then moves all files in __deploycache to deploy directory
    /// </summary>
    private void RestoreCache()
    {
        // First remove the already deployed files
        foreach (var filePath in _deployedFiles) File.Delete(filePath);
        // Moves all files in cache to deploy dir
        // DirectoryInfo cacheDir = new DirectoryInfo(_cachePath);
        foreach (var filePath in _cachedFiles)
        {
            var subPath = Path.GetRelativePath(_cachePath, filePath);
            var finalPath = Path.Combine(_deployPath, subPath);
            File.Move(filePath, finalPath);
        }

        _deployedFiles = new List<string>();
        SaveLinkedMods();
    }

    private void LinkMods(List<Mod?> mods)
    {
        double fileNumber = 0;
        foreach (var mod in mods)
        {
            if (mod == null) continue;
            var modDir = new DirectoryInfo(mod.ModPath);
            foreach (var file in modDir.GetFiles("*", SearchOption.AllDirectories))
            {
                LinkFile(file, modDir);
                var args = new FileDeployedArgs();
                args.Progress = fileNumber;
                OnFileLinked(args);
                fileNumber++;
            }
        }
    }

    private void LinkFile(FileInfo file, DirectoryInfo modDir)
    {
        var subPath = Path.GetRelativePath(modDir.FullName, file.FullName); // to remove the mods directory + mod name
        var finalPath = Path.Combine(_deployPath, subPath); // final path to deploy to, including file name

        //if the file already exists, and was not deployed by NexNux, we move it to the deploy cache
        if (File.Exists(finalPath) && !_deployedFiles.Exists(p => p == finalPath))
        {
            var cacheFile = Path.Combine(_cachePath, Path.GetFileName(finalPath));
            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile) ?? throw new InvalidOperationException());
            File.Move(finalPath, cacheFile);
            _cachedFiles.Add(cacheFile);
        }

        // if the file already exists and was deployed already by nexnux
        else if (File.Exists(finalPath) && _deployedFiles.Exists(p => p == finalPath))
        {
            File.Delete(finalPath);
            _deployedFiles.Remove(finalPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? throw new InvalidOperationException());
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            CreateHardLink(finalPath, file.FullName, IntPtr.Zero);
        else
            File.CreateSymbolicLink(finalPath, file.FullName);
        _deployedFiles.Add(finalPath);
    }

    private void SaveLinkedMods()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_jsonPath) ?? throw new InvalidOperationException());
        using var createStream = File.Create(_jsonPath);
        JsonSerializer.Serialize(createStream, _deployedFiles, typeof(List<string>), StringsSerializerContext.Default);
        createStream.Dispose();
    }

    private void LoadLinkedMods()
    {
        if (!File.Exists(_jsonPath))
            SaveLinkedMods();
        var jsonString = File.ReadAllText(_jsonPath);
        _deployedFiles =
            JsonSerializer.Deserialize(jsonString, typeof(List<string>), StringsSerializerContext.Default) as
                List<string>
            ?? throw new InvalidOperationException();
    }

    private void OnFileLinked(FileDeployedArgs e)
    {
        if (FileDeployed == null) return;
        var handler = FileDeployed;
        handler?.Invoke(this, e);
    }
}