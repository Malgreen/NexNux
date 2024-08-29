using NexNux.Core.Models;
using NexNux.Core.Repositories;

namespace NexNux.Core.Services;

public class ModService
{
    private readonly IModRepository _repository;

    public ModService(Game game)
    {
        _repository = new ModRepositoryJson(game);
    }

    public ModService(Game game, IModRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Mod>> GetAll()
    {
        return await Task.Run(() => _repository.GetMods());
    }

    public async Task<bool> SetAll(List<Mod> mods)
    {
        return await Task.Run(() => _repository.SetMods(mods));
    }

    public async Task<bool> Add(Mod mod)
    {
        return await Task.Run(() => _repository.AddMod(mod));
    }

    public async Task<bool> Remove(Mod mod)
    {
        return await Task.Run(() => _repository.RemoveModById(mod.Id));
    }

    public async Task<bool> Modify(Mod mod)
    {
        return await Task.Run(() => _repository.ModifyMod(mod));
    }

    public async Task<bool> Reorder(int oldIndex, int newIndex)
    {
        return await Task.Run(() => _repository.ReorderModByIndices(oldIndex, newIndex));
    }
}