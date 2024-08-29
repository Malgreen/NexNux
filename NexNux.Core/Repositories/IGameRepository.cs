using NexNux.Core.Models;

namespace NexNux.Core.Repositories;

public interface IGameRepository
{
    public List<Game> GetGames();
    public Game? GetGameById(Guid gameId);
    public bool AddGame(Game game);
    public bool RemoveGameById(Guid gameId);
    public bool ModifyGame(Game game);
}