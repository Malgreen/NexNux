using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace NexNux.Models.Gamebryo;

public class GamebryoPluginList
{
    public GamebryoPluginList(string deployDir, string settingsDir, string appDataDir, GameType gameType)
    {
        _pluginTypeDictionary = new Dictionary<string, GamebryoPluginType>
        {
            {".esl", GamebryoPluginType.ESL},
            {".esp", GamebryoPluginType.ESP},
            {".esm", GamebryoPluginType.ESM}
        };

        _deployDirPath = deployDir;
        _pluginListFileName = Path.Combine(settingsDir, "PluginList.json");
        _pluginsTxtPath = Path.Combine(appDataDir, "plugins.txt");
        _loadorderTxtPath = Path.Combine(appDataDir, "loadorder.txt");
        _gameType = gameType;
        Plugins = new ObservableCollection<GamebryoPlugin>();
        Load();
    }
    
    public ObservableCollection<GamebryoPlugin> Plugins { get; set; }
    private readonly GameType _gameType;
    private readonly Dictionary<string, GamebryoPluginType> _pluginTypeDictionary;
    private readonly string _pluginListFileName;
    private readonly string _pluginsTxtPath;
    private readonly string _loadorderTxtPath;
    private readonly string _deployDirPath;

    /// <summary>
    /// <para>Save the Plugins collection.</para>
    /// <para>1. Sets each plugin's load order index to their index in the Plugins collection.</para>
    /// <para>2. Sets correct timestamps on plugin files according to their loadorder.</para>
    /// <para>3. Serializes and writes the Plugins collection to the corresponding PluginList.json.</para>
    /// <para>4. Writes plugins to both plugins.txt and loadorder.txt found in the game's AppData directory.</para>
    /// </summary>
    public void Save() 
    {
        SetIndices();
        SetPluginTimeStamps();
        using FileStream createStream = File.Create(_pluginListFileName);
        JsonSerializer.Serialize(createStream, Plugins, new JsonSerializerOptions(){ WriteIndented = true });
        createStream.Dispose();
        SavePluginsToFile(_pluginsTxtPath, _gameType == GameType.BGSPostSkyrim);
        SavePluginsToFile(_loadorderTxtPath, false);
    }
    
    /// <summary>
    /// <para>Load/create the Plugins collection.</para>
    /// <para>1. If a PluginList.json exists, that is deserialized into the Plugins collection - otherwise, the plugins.txt
    /// file is read and put into the collection.</para>
    /// <para>2. Refreshes the Plugins collection according to Deploy directory</para>
    /// <para>3. Refreshes the Plugins collection according to the 'plugins.txt' file</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">Throws deserialization error</exception>
    public void Load()
    {
        try
        {
            string jsonString = File.ReadAllText(_pluginListFileName);
            Plugins = JsonSerializer.Deserialize<ObservableCollection<GamebryoPlugin>>(jsonString) ??
                      throw new InvalidOperationException();
        }
        catch (FileNotFoundException)
        {
            Plugins = new ObservableCollection<GamebryoPlugin>(GetPluginsFromFile(_pluginsTxtPath));
        }
        catch (JsonException)
        {
            File.Delete(_pluginListFileName);
        }

        RefreshFromDeployDirectory();
        RefreshFromTxt();
    }

    /// <summary>
    /// <para>Refreshes the Plugins collection to reflect the plugins and their order found in the game's plugins.txt.</para>
    /// <para>Useful if load order has been changed by an external tool.</para>
    /// </summary>
    public void RefreshFromTxt()
    {
        List<GamebryoPlugin> pluginsTxtPlugins = GetPluginsFromFile(_pluginsTxtPath);
        List<GamebryoPlugin> enabledPlugins = Plugins.Where(plugin => plugin.IsEnabled).ToList();
        foreach(GamebryoPlugin plugin in enabledPlugins)
        {
            int pluginIndexInFile = pluginsTxtPlugins.FindIndex(p => p.Equals(plugin));
            int pluginIndexInEnabledList = enabledPlugins.IndexOf(plugin);
            int pluginIndexInFullList = Plugins.IndexOf(plugin);
            int indexDifference = pluginIndexInFullList - pluginIndexInEnabledList;

            Plugins.Move(pluginIndexInFullList, pluginIndexInFile + indexDifference);
            enabledPlugins = Plugins.Where(p => p.IsEnabled).ToList();
        }
    }
    
    /// <summary>
    /// <para>Refreshes the Plugins collection according to the actual contents of the deploy folder, removing any from the list
    /// whose files are no longer present, and adding new file plugins to the collection.</para>
    /// <para>Useful when one or more mod(s) containing new plugins have been deployed.</para>
    /// </summary>
    public void RefreshFromDeployDirectory()
    {
        List<GamebryoPlugin> pluginsTxtPlugins = GetPluginsFromFile(_pluginsTxtPath);
        List<GamebryoPlugin> deployDirPlugins = GetPluginsFromDirectory(_deployDirPath);

        // Check plugins that are present in the file but not in the deploy directory
        // Remove these plugins
        List<GamebryoPlugin> absentPlugins = pluginsTxtPlugins.Except(deployDirPlugins).ToList();
        Plugins = new ObservableCollection<GamebryoPlugin>(Plugins.Except(absentPlugins).ToList());

        // Get new plugins that are present in the deploy directory, but not in the file
        // These plugins can just be added to the list
        List<GamebryoPlugin> newPlugins = deployDirPlugins.Except(pluginsTxtPlugins).ToList();
        Plugins = new ObservableCollection<GamebryoPlugin>(Plugins.Union(newPlugins).ToList());
        Save();
    }

    private void SavePluginsToFile(string filePath, bool useAsteriskPrefix)
    {
        using StreamWriter streamWriter = new StreamWriter(filePath);
        foreach(GamebryoPlugin plugin in Plugins)
        {
            if (!plugin.IsEnabled) continue;
            string pluginName = useAsteriskPrefix ? string.Concat("*", plugin.PluginName) : plugin.PluginName;
            streamWriter.WriteLine(pluginName);
        }
    }

    private List<GamebryoPlugin> GetPluginsFromFile(string filePath)
    {
        List<GamebryoPlugin> readPlugins = new List<GamebryoPlugin>();
        using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
        {
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                while (!streamReader.EndOfStream)
                {
                    string pluginLine = streamReader.ReadLine() ?? string.Empty;
                    string pluginName = pluginLine.StartsWith("*") ? pluginLine.Substring(1) : pluginLine;
                    
                    // A bit risky, but should work most of the time - change to bool parameter if necessary
                    if (!_pluginTypeDictionary.TryGetValue(Path.GetExtension(pluginLine.ToLower()), out var pluginType)) continue;
                    
                    GamebryoPlugin plugin = new GamebryoPlugin(pluginName, pluginType, readPlugins.Count, true);
                    readPlugins.Add(plugin);
                }
            }
        }
        
        return readPlugins;
    }

    private List<GamebryoPlugin> GetPluginsFromDirectory(string dirPath)
    {
        List<GamebryoPlugin> readPlugins = new List<GamebryoPlugin>();
        
        DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
        List<FileInfo> dirFiles = directoryInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).ToList();
        foreach (FileInfo file in dirFiles)
        {
            if (!_pluginTypeDictionary.TryGetValue(file.Extension.ToLower(), out var pluginType)) continue;
            GamebryoPlugin plugin = new GamebryoPlugin(file.Name, pluginType, readPlugins.Count, true);
            readPlugins.Add(plugin);
        }
        
        return readPlugins;
    }
    

    private void SetTimestampsAndIndices()
    {
        // This should probably be used instead of SetIndices and SetTimeStamps,
        // as they both have a foreach loop iterating over the same collection
        foreach (GamebryoPlugin plugin in Plugins)
        {
            plugin.LoadOrderIndex = Plugins.IndexOf(plugin);
            string pluginPath = Path.Combine(_deployDirPath, plugin.PluginName);
            File.SetLastWriteTime(pluginPath, DateTime.Now);
        }
    }

    private void SetIndices()
    {
        foreach (GamebryoPlugin plugin in Plugins)
        {
            plugin.LoadOrderIndex = Plugins.IndexOf(plugin);
        }
    }

    private void SetPluginTimeStamps()
    {
        int timeOffset = 0;
        foreach (GamebryoPlugin plugin in Plugins)
        {
            string pluginPath = Path.Combine(_deployDirPath, plugin.PluginName);
            File.SetLastWriteTime(pluginPath, new DateTime(2000 + timeOffset, 1, 1));
            timeOffset++;
        }
    }
}