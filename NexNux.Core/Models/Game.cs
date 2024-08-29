using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using NexNux.Core.Models.Bgs;

namespace NexNux.Core.Models;

[JsonDerivedType(typeof(Game), nameof(Game))]
[JsonDerivedType(typeof(BgsGame), nameof(BgsGame))]
[JsonDerivedType(typeof(BgsGamePostSkyrim), nameof(BgsGamePostSkyrim))]
public partial class Game : ObservableObject
{
    public readonly Guid Id;

    [ObservableProperty] private string _gameDirectory;

    [ObservableProperty] private string _name;

    [ObservableProperty] private string _nexNuxDirectory;

    public Game(Guid id, string name, string gameDirectory, string nexNuxDirectory)
    {
        Id = id;
        Name = name;
        GameDirectory = gameDirectory;
        NexNuxDirectory = nexNuxDirectory;
    }
}