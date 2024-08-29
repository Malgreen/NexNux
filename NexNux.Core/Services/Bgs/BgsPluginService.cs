using NexNux.Core.Models.Bgs;
using NexNux.Core.Repositories.Bgs;

namespace NexNux.Core.Services.Bgs;

public class BgsPluginService
{
    private readonly IBgsPluginRepository _repository;

    public BgsPluginService(BgsGame bgsGame)
    {
        _repository = new BgsPluginRepositoryJson(bgsGame);
    }

    public BgsPluginService(BgsGame bgsGame, IBgsPluginRepository bgsPluginRepository)
    {
        _repository = bgsPluginRepository;
    }

    public async Task<List<BgsPlugin>> GetAll()
    {
        return await Task.Run(() => _repository.GetBgsPlugins());
    }

    public async Task<bool> Update(BgsPlugin bgsPlugin)
    {
        return await Task.Run(() => _repository.UpdateBgsPlugin(bgsPlugin));
    }

    public async Task<bool> Reorder(int oldIndex, int newIndex)
    {
        return await Task.Run(() => _repository.ReorderBgsPluginByIndices(oldIndex, newIndex));
    }
}