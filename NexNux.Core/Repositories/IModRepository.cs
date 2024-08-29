using NexNux.Core.Models;

namespace NexNux.Core.Repositories;

public interface IModRepository
{
    public List<Mod> GetMods();
    public bool SetMods(List<Mod> mods);
    public Mod? GetModById(Guid modId);
    public bool AddMod(Mod mod);
    public bool RemoveModById(Guid modId);
    public bool ModifyMod(Mod mod);
    public bool ReorderModByIndices(int oldIndex, int newIndex);
}