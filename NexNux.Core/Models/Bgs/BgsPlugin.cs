using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NexNux.Core.Models.Bgs;

[JsonDerivedType(typeof(BgsPluginEsp), nameof(BgsPluginEsp))]
[JsonDerivedType(typeof(BgsPluginEsl), nameof(BgsPluginEsl))]
[JsonDerivedType(typeof(BgsPluginEsm), nameof(BgsPluginEsm))]
public abstract partial class BgsPlugin : ObservableObject
{
    public readonly string Name;

    public readonly string Path;

    [ObservableProperty] private bool _isEnabled;

    protected BgsPlugin(string name, string path, bool isEnabled)
    {
        Name = name;
        Path = path;
        IsEnabled = isEnabled;
    }
}