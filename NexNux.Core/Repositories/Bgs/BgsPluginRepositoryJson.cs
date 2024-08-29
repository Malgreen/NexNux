using NexNux.Core.Models.Bgs;
using NexNux.Core.Utilities.Serialization;

namespace NexNux.Core.Repositories.Bgs;

public class BgsPluginRepositoryJson : IBgsPluginRepository
{
    private readonly Type _bgsGameType;
    private readonly string _gameDataPath;

    private readonly string _jsonPath;
    private readonly string _loadOrderTxtPath;
    private readonly string _pluginsTxtPath;

    public BgsPluginRepositoryJson(BgsGame bgsGame)
    {
        _jsonPath = Path.Combine(bgsGame.NexNuxDirectory, "plugins.json");
        _pluginsTxtPath = Path.Combine(bgsGame.AppDataDirectory, "plugins.txt");
        _loadOrderTxtPath = Path.Combine(bgsGame.AppDataDirectory, "loadorder.txt");
        _gameDataPath = bgsGame.GameDirectory;
        _bgsGameType = bgsGame.GetType();
    }

    public List<BgsPlugin> GetBgsPlugins()
    {
        SyncBgsPluginsWithDisc();
        return DeserializeJson();
    }

    public bool UpdateBgsPlugin(BgsPlugin bgsPlugin)
    {
        var plugins = DeserializeJson();
        var index = plugins.FindIndex(p => p.Name == bgsPlugin.Name);
        if (index == -1)
            return false;
        plugins[index].IsEnabled = bgsPlugin.IsEnabled;

        SerializeJson(plugins);
        WriteBgsPluginNamesToTxtFiles(plugins);
        SetTimestamps(plugins);
        return true;
    }

    public bool ReorderBgsPluginByIndices(int oldIndex, int newIndex)
    {
        var plugins = DeserializeJson();
        var plugin = plugins[oldIndex];
        plugins.RemoveAt(oldIndex);
        plugins.Insert(newIndex, plugin);

        SerializeJson(plugins);
        WriteBgsPluginNamesToTxtFiles(plugins);
        SetTimestamps(plugins);
        return true;
    }

    private bool SyncBgsPluginsWithDisc()
    {
        var plugins = DeserializeJson();
        var folderPlugins = ReadBgsPluginsFromGameFolder();
        var txtPlugins = ReadBgsPluginsFromTxtFile();

        var absentPlugins = txtPlugins.Except(folderPlugins);
        var newPlugins = folderPlugins.Except(txtPlugins);

        plugins = plugins.Except(absentPlugins).ToList();
        plugins.AddRange(newPlugins);

        SerializeJson(plugins);
        WriteBgsPluginNamesToTxtFiles(plugins);
        SetTimestamps(plugins);
        return true;
    }

    private List<BgsPlugin> DeserializeJson()
    {
        if (!File.Exists(_jsonPath))
            JsonListHelper.CreateJsonFromList(ReadBgsPluginsFromTxtFile(), _jsonPath,
                BgsPluginsSerializerContext.Default.ListBgsPlugin);
        return JsonListHelper.DeserializeJsonToList(_jsonPath, BgsPluginsSerializerContext.Default.ListBgsPlugin);
    }

    private void SerializeJson(List<BgsPlugin> bgsPlugins)
    {
        if (!File.Exists(_jsonPath))
            JsonListHelper.CreateJsonFromList(bgsPlugins, _jsonPath, BgsPluginsSerializerContext.Default.ListBgsPlugin);
        JsonListHelper.SerializeListToJson(bgsPlugins, _jsonPath, BgsPluginsSerializerContext.Default.ListBgsPlugin);
    }

    private List<BgsPlugin> ReadBgsPluginsFromGameFolder()
    {
        var plugins = new List<BgsPlugin>();

        var directoryInfo = new DirectoryInfo(_gameDataPath);
        var dirFiles = directoryInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).ToList();
        foreach (var file in dirFiles)
        {
            var plugin = GeneratePluginFromFileName(file.Name);
            if (plugin is null)
                continue;
            plugins.Add(plugin);
        }

        return plugins;
    }

    private List<BgsPlugin> ReadBgsPluginsFromTxtFile()
    {
        var plugins = new List<BgsPlugin>();
        using var fileStream = File.OpenRead(_pluginsTxtPath);
        using var streamReader = new StreamReader(fileStream);

        while (!streamReader.EndOfStream)
        {
            var pluginLine = streamReader.ReadLine() ?? string.Empty;
            var pluginName = pluginLine.StartsWith("*") ? pluginLine.Substring(1) : pluginLine;
            var plugin = GeneratePluginFromFileName(pluginName);
            if (plugin is null)
                continue;
            plugins.Add(plugin);
        }

        return plugins;
    }

    private void WriteBgsPluginNamesToTxtFiles(List<BgsPlugin> bgsPlugins)
    {
        using var pluginsTxtWriter = new StreamWriter(_pluginsTxtPath);
        using var loadorderTxtWriter = new StreamWriter(_loadOrderTxtPath);
        foreach (var plugin in bgsPlugins.Where(plugin => plugin.IsEnabled))
        {
            pluginsTxtWriter.WriteLine(_bgsGameType == typeof(BgsGamePostSkyrim)
                ? string.Concat("*", plugin.Name)
                : plugin.Name);
            loadorderTxtWriter.WriteLine(plugin.Name);
        }
    }

    private void SetTimestamps(List<BgsPlugin> bgsPlugins)
    {
        var timeOffset = 0;
        foreach (var plugin in bgsPlugins.Where(plugin => plugin.IsEnabled))
        {
            File.SetLastWriteTime(plugin.Path, new DateTime(2000, 1, 1).AddDays(timeOffset));
            timeOffset++;
        }
    }

    private BgsPlugin? GeneratePluginFromFileName(string fileName)
    {
        return Path.GetExtension(fileName).ToLower() switch
        {
            ".esp" => new BgsPluginEsp(fileName, Path.Combine(_gameDataPath, fileName), true),
            ".esl" => new BgsPluginEsl(fileName, Path.Combine(_gameDataPath, fileName), true),
            ".esm" => new BgsPluginEsm(fileName, Path.Combine(_gameDataPath, fileName), true),
            _ => null
        };
    }
}