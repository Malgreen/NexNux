using NexNux.Core.Models;
using NexNux.Deployment.Models;

namespace NexNux.Deployment.Repositories;

public interface ILinkDeploymentRepository
{
    public bool LinkModsBottomUp(List<Mod> mods);
    public bool RestoreCache();
    public event EventHandler<DeployingModEventArgs> DeployingMod;
}