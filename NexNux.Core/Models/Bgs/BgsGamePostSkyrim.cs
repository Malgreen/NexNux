namespace NexNux.Core.Models.Bgs;

public class BgsGamePostSkyrim : BgsGame
{
    public BgsGamePostSkyrim(Guid id, string name, string gameDirectory, string nexNuxDirectory,
        string appDataDirectory) : base(id, name, gameDirectory, nexNuxDirectory, appDataDirectory)
    {
    }
}