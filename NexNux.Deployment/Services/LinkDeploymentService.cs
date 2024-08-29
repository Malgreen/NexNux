using NexNux.Core.Models;
using NexNux.Deployment.Models;
using NexNux.Deployment.Repositories;

namespace NexNux.Deployment.Services;

public class LinkDeploymentService : IDeploymentService
{
    private readonly ILinkDeploymentRepository _repository;

    public LinkDeploymentService(Game game)
    {
        _repository = new LinkDeploymentRepository(game);
        _repository.DeployingMod += OnDeployingMod;
    }

    public LinkDeploymentService(ILinkDeploymentRepository repository)
    {
        _repository = repository;
        _repository.DeployingMod += OnDeployingMod;
    }

    public event EventHandler<DeployingModEventArgs>? DeployingMod;

    public async Task<bool> Deploy(List<Mod> mods)
    {
        return await Task.Run(() => _repository.RestoreCache() && _repository.LinkModsBottomUp(mods));
    }

    public async Task<bool> Clear()
    {
        return await Task.Run(() => _repository.RestoreCache());
    }

    private void OnDeployingMod(object? sender, DeployingModEventArgs e)
    {
        DeployingMod?.Invoke(this, e);
    }
}