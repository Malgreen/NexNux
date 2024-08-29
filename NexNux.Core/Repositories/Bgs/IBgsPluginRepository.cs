using NexNux.Core.Models.Bgs;

namespace NexNux.Core.Repositories.Bgs;

public interface IBgsPluginRepository
{
    public List<BgsPlugin> GetBgsPlugins();
    public bool UpdateBgsPlugin(BgsPlugin bgsPlugin);
    public bool ReorderBgsPluginByIndices(int oldIndex, int newIndex);
}