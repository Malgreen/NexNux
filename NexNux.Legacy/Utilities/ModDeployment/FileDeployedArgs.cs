using System;

namespace NexNux.Legacy.Utilities.ModDeployment;

public class FileDeployedArgs : EventArgs
{
    public double Progress { get; set; }
}