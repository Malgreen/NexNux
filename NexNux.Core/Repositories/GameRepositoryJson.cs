using NexNux.Core.Models;
using NexNux.Core.Models.Bgs;
using NexNux.Core.Utilities.Serialization;

namespace NexNux.Core.Repositories;

public class GameRepositoryJson : IGameRepository
{
    private readonly string _jsonPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NexNux", "games.json");

    public List<Game> GetGames()
    {
        return DeserializeJson();
    }

    public Game? GetGameById(Guid gameId)
    {
        return DeserializeJson().Find(g => g.Id == gameId);
    }

    public bool AddGame(Game game)
    {
        var games = DeserializeJson();
        games.Add(game);
        SerializeJson(games);
        return true;
    }

    public bool RemoveGameById(Guid gameId)
    {
        var games = DeserializeJson();
        games = games.Where(g => g.Id != gameId).ToList();
        SerializeJson(games);
        return true;
    }

    public bool ModifyGame(Game game)
    {
        var games = DeserializeJson();
        var index = games.FindIndex(g => g.Id == game.Id);
        if (index == -1)
            return false;
        games[index].Name = game.Name;
        games[index].GameDirectory = game.GameDirectory;
        games[index].NexNuxDirectory = game.NexNuxDirectory;
        if (game is BgsGame bgsGame && games[index] is BgsGame)
            ((BgsGame)games[index]).AppDataDirectory = bgsGame.AppDataDirectory;
        SerializeJson(games);
        return true;
    }

    private List<Game> DeserializeJson()
    {
        if (!File.Exists(_jsonPath))
            JsonListHelper.CreateJsonFromList(new List<Game>(), _jsonPath, GamesSerializerContext.Default.ListGame);
        return JsonListHelper.DeserializeJsonToList(_jsonPath, GamesSerializerContext.Default.ListGame);
    }

    private void SerializeJson(List<Game> games)
    {
        if (!File.Exists(_jsonPath))
            JsonListHelper.CreateJsonFromList(games, _jsonPath, GamesSerializerContext.Default.ListGame);
        JsonListHelper.SerializeListToJson(games, _jsonPath, GamesSerializerContext.Default.ListGame);
    }
}