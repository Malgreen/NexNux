using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexNux.Legacy.Models;

namespace NexNux.Legacy.Utilities.ModDeployment;

public interface IModDeployer
{
    public Game CurrentGame { get; }
    public event EventHandler<FileDeployedArgs> FileDeployed;
    public Task Deploy(List<Mod?> mods);
    public Task Clear();
}