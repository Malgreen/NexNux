using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexNux.Legacy.Utilities;

namespace NexNux.Legacy.Models;

public class GameSettings
{
    private readonly string? _settingsFileName;

    public GameSettings(string settingsDir)
    {
        _settingsFileName = Path.Combine(settingsDir, "Settings.json");
        Load();
    }

    [JsonConstructor]
    public GameSettings()
    {
    }

    public bool RecentlyDeployed
    {
        get;
        set;
        // Saving on property change should be possible, but not always necessary.
        // It also creates complications when loading.
        //Save();
    }

    public void Load()
    {
        if (!File.Exists(_settingsFileName))
        {
            Initialize();
        }
        else
        {
            var jsonString = File.ReadAllText(_settingsFileName);
            var loadedSettings =
                JsonSerializer.Deserialize(jsonString, typeof(GameSettings), GameSettingsSerializerContext.Default) as
                    GameSettings;
            if (loadedSettings == null) return;

            // Below all properties should be set to the loaded objects fields
            RecentlyDeployed = loadedSettings.RecentlyDeployed;
        }
    }

    public void Save()
    {
        if (_settingsFileName == null) return;
        using var createStream = File.Create(_settingsFileName);
        JsonSerializer.Serialize(createStream, this, typeof(GameSettings), GameSettingsSerializerContext.Default);
        createStream.Dispose();
    }

    /// <summary>
    ///     Initializes all properties of the settings to default values, and serializes the object.
    /// </summary>
    private void Initialize()
    {
        RecentlyDeployed = false;
        Save();
    }
}