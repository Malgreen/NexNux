using NexNux.Core.Models;
using NexNux.Core.Utilities.Serialization;

namespace NexNux.Core.Repositories;

public class ModRepositoryJson : IModRepository
{
    private readonly string _jsonPath;

    public ModRepositoryJson(Game game)
    {
        _jsonPath = Path.Combine(game.NexNuxDirectory, "mods.json");
    }

    public List<Mod> GetMods()
    {
        return DeserializeJson();
    }

    public bool SetMods(List<Mod> mods)
    {
        SerializeJson(mods);
        return true;
    }

    public Mod? GetModById(Guid modId)
    {
        return DeserializeJson().Find(m => m.Id == modId);
    }

    public bool AddMod(Mod mod)
    {
        var mods = DeserializeJson();
        mods.Add(mod);
        SerializeJson(mods);
        return true;
    }

    public bool RemoveModById(Guid modId)
    {
        var mods = DeserializeJson();
        mods = mods.Where(m => m.Id != modId).ToList();
        SerializeJson(mods);
        return true;
    }

    public bool ModifyMod(Mod mod)
    {
        var mods = DeserializeJson();
        var index = mods.FindIndex(m => m.Id == mod.Id);
        if (index == -1)
            return false;
        mods[index].Name = mod.Name;
        mods[index].Path = mod.Path;
        mods[index].IsEnabled = mod.IsEnabled;
        SerializeJson(mods);
        return true;
    }

    public bool ReorderModByIndices(int oldIndex, int newIndex)
    {
        var mods = DeserializeJson();
        var mod = mods[oldIndex];
        mods.RemoveAt(oldIndex);
        mods.Insert(newIndex, mod);
        SerializeJson(mods);
        return true;
    }

    private List<Mod> DeserializeJson()
    {
        if (!File.Exists(_jsonPath))
            JsonListHelper.CreateJsonFromList(new List<Mod>(), _jsonPath, ModsSerializerContext.Default.ListMod);
        return JsonListHelper.DeserializeJsonToList(_jsonPath, ModsSerializerContext.Default.ListMod);
    }

    private void SerializeJson(List<Mod> mods)
    {
        if (!File.Exists(_jsonPath))
            JsonListHelper.CreateJsonFromList(new List<Mod>(), _jsonPath, ModsSerializerContext.Default.ListMod);
        JsonListHelper.SerializeListToJson(mods, _jsonPath, ModsSerializerContext.Default.ListMod);
    }
}