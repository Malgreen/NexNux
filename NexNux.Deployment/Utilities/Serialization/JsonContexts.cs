using System.Text.Json.Serialization;

namespace NexNux.Deployment.Utilities.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<string>))]
internal partial class StringsSerializerContext : JsonSerializerContext
{
}