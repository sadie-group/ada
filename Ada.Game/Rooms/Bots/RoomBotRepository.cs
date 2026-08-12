using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Serilog;

namespace Ada.Game.Rooms.Bots;

public class RoomBotRepository : IRoomBotRepository
{
    private readonly ConcurrentDictionary<int, IRoomBot> _bots = new();

    public ICollection<IRoomBot> GetAll() => _bots.Values;
    public bool TryAdd(IRoomBot bot) => _bots.TryAdd(bot.Bot.Id, bot);
    public bool TryGetById(int id, out IRoomBot? bot) => _bots.TryGetValue(id, out bot);
    public bool TryRemove(int id, out IRoomBot? bot) => _bots.TryRemove(id, out bot);
    public int Count => _bots.Count;
    
    public async Task RunPeriodicCheckAsync()
    {
        foreach (var bot in _bots.Values)
        {
            try
            {
                await bot.RunPeriodicCheckAsync();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Periodic check failed for a bot");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
    }
}