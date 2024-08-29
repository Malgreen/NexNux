using System.Text.Json.Serialization;
using NexNux.Core.Models;
using NexNux.Core.Models.Bgs;

namespace NexNux.Core.Utilities.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<Game>))]
internal partial class GamesSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<Mod>))]
internal partial class ModsSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<BgsPlugin>))]
internal partial class BgsPluginsSerializerContext : JsonSerializerContext
{
}