using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NexNux.Core.Utilities.Serialization;

public static class JsonListHelper
{
    public static void CreateJsonFromList<T>(List<T> list, string jsonPath, JsonTypeInfo<List<T>> jsonTypeInfo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath) ?? throw new InvalidOperationException());
        File.Create(jsonPath);
        SerializeListToJson(list, jsonPath, jsonTypeInfo);
    }

    public static void SerializeListToJson<T>(List<T> list, string jsonPath, JsonTypeInfo<List<T>> jsonTypeInfo)
    {
        using var fileStream = File.Create(jsonPath);
        JsonSerializer.Serialize(fileStream, list, jsonTypeInfo);
        fileStream.Dispose();
    }

    public static List<T> DeserializeJsonToList<T>(string jsonPath, JsonTypeInfo<List<T>> jsonTypeInfo)
    {
        using var textStream = File.OpenRead(jsonPath);
        var bgsPlugins = JsonSerializer.Deserialize(textStream, jsonTypeInfo) ?? new List<T>();
        textStream.Dispose();
        return bgsPlugins;
    }
}