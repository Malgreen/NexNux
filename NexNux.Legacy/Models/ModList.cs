using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using NexNux.Legacy.Utilities;

namespace NexNux.Legacy.Models;

public class ModList
{
    private readonly string _modListFileName;

    public ModList(string settingsDir)
    {
        _modListFileName = Path.Combine(settingsDir, "ModList.json");
        Mods = LoadList();
    }

    public List<Mod?> Mods { get; set; }

    public void SaveList()
    {
        try
        {
            using var createStream = File.Create(_modListFileName);
            JsonSerializer.Serialize(createStream, Mods, typeof(List<Mod>), ModsSerializerContext.Default);
            createStream.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
            throw
                new UnauthorizedAccessException(ex
                    .Message); // these nested exceptions are awful, pls fix (we also have try/catch in game config VM)
        }
    }

    public List<Mod?> LoadList()
    {
        var loadedMods = new List<Mod?>();

        if (!File.Exists(_modListFileName))
        {
            Mods = new List<Mod?>();
            SaveList();
        }
        else
        {
            var jsonString = File.ReadAllText(_modListFileName);
            loadedMods =
                JsonSerializer.Deserialize(jsonString, typeof(List<Mod>),
                    ModsSerializerContext.Default) as List<Mod?> ?? throw new InvalidOperationException();
        }

        return loadedMods;
    }

    public List<Mod?> GetActiveMods()
    {
        return new List<Mod?>(Mods.Where(d => d is { Enabled: true }));
    }

    public void InstallMod(Mod mod, string rootPath)
    {
        throw new NotImplementedException();
    }

    public void ModifyMod(string modName, string modPath, double fileSize, long index, bool enabled)
    {
        throw new NotImplementedException();
    }

    public void UninstallMod(Mod mod)
    {
        throw new NotImplementedException();
    }
}