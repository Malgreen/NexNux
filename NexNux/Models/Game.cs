﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using NexNux.Models.Gamebryo;

namespace NexNux.Models;

public class Game
{
    public Game(string gameName, GameType type, string deployDirectory, string modsDirectory, string? appDataDirectory)
    {
        GameName = gameName;
        Type = type;
        DeployDirectory = deployDirectory;
        ModsDirectory = modsDirectory;
        SettingsDirectory = Path.Combine(ModsDirectory, ".NexNux");
        AppDataDirectory = appDataDirectory;
        ValidateInfo();

        Settings = new GameSettings(SettingsDirectory ?? throw new InvalidOperationException());
        _modList = new ModList(SettingsDirectory ?? throw new InvalidOperationException());
        if (Type is GameType.BGS or GameType.BGSPostSkyrim && AppDataDirectory is not null)
        {
            _gamebryoPluginList = new GamebryoPluginList(DeployDirectory, SettingsDirectory, AppDataDirectory, Type);
        }
    }

    public string GameName { get; set; }
    public GameType Type { get; set; }
    public string DeployDirectory { get; set; }
    public string ModsDirectory { get; set; }
    public string SettingsDirectory { get; set; }
    public string? AppDataDirectory { get; set; }

    [JsonIgnore]
    public GameSettings Settings { get; private set; }
    private ModList _modList;
    private GamebryoPluginList? _gamebryoPluginList;

    void ValidateInfo()
    {
        if (GameName.Equals(string.Empty))
            throw new Exception("Game must have a name");

        FileInfo deployInfo = new FileInfo(DeployDirectory);
        FileInfo modsInfo = new FileInfo(ModsDirectory);
        if (!Equals(Path.GetPathRoot(deployInfo.FullName), Path.GetPathRoot(modsInfo.FullName)))
            throw new Exception("Directories must reside on the same drive"); // Hardlink deployment cannot be done if different drives

        Directory.CreateDirectory(DeployDirectory);
        Directory.CreateDirectory(ModsDirectory);
        Directory.CreateDirectory(SettingsDirectory);
    }

    public List<Mod?> GetAllMods()
    {
        return _modList.Mods;
    }

    public List<Mod?> GetActiveMods()
    {
        return _modList.GetActiveMods();
    }

    /// <summary>
    /// Deletes all files and folders from the Game's 'mods' directory
    /// </summary>
    /// TODO: When mod deployment is implemented, should also restore all original files first
    public void DeleteMods()
    {
        DirectoryInfo dirInfo = new DirectoryInfo(ModsDirectory);

        foreach (FileInfo file in dirInfo.GetFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo dir in dirInfo.GetDirectories())
        {
            dir.Delete(true);
        }
    }

    public override string ToString()
    {
        return GameName;
    }
}