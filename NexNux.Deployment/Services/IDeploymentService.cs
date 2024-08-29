using NexNux.Core.Models;
using NexNux.Deployment.Models;

namespace NexNux.Deployment.Services;

public interface IDeploymentService
{
    public event EventHandler<DeployingModEventArgs> DeployingMod;
    public Task<bool> Deploy(List<Mod> mods);
    public Task<bool> Clear();
}