using CommunityToolkit.Mvvm.ComponentModel;

namespace NexNux.Core.Models;

public partial class Mod : ObservableObject
{
    public readonly Guid Id;

    [ObservableProperty] private bool _isEnabled;

    [ObservableProperty] private string _name;

    [ObservableProperty] private string _path;

    public Mod(Guid id, string name, string path, bool isEnabled)
    {
        Id = id;
        Name = name;
        Path = path;
        IsEnabled = isEnabled;
    }
}