using NexNux.Core.Models;
using NexNux.Core.Repositories;

namespace NexNux.Core.Services;

public class GameService
{
    private readonly IGameRepository _repository;

    public GameService()
    {
        _repository = new GameRepositoryJson();
    }

    public GameService(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Game>> GetAll()
    {
        return await Task.Run(() => _repository.GetGames());
    }

    public async Task<Game?> GetById(Guid id)
    {
        return await Task.Run(() => _repository.GetGameById(id));
    }

    public async Task<bool> Add(Game game)
    {
        return await Task.Run(() => _repository.AddGame(game));
    }

    public async Task<bool> Remove(Game game)
    {
        return await Task.Run(() => _repository.RemoveGameById(game.Id));
    }

    public async Task<bool> Modify(Game game)
    {
        return await Task.Run(() => _repository.ModifyGame(game));
    }
}