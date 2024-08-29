namespace NexNux.Deployment.Models;

public class DeployingModEventArgs : EventArgs
{
    public DeployingModEventArgs(string modName, int modNumber, int modAmount)
    {
        ModName = modName;
        ModNumber = modNumber;
        ModAmount = modAmount;
    }

    public string ModName { get; set; }
    public int ModNumber { get; set; }
    public int ModAmount { get; set; }
}