using Ada.API.Interfaces.Game.Rooms;
using Ada.Game.Rooms.Filter;
using Ada.API.Interfaces.Game.WordFilter;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.WordFilter;

public static class WordFilterServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IWordFilterService, WordFilterService>();
        serviceCollection.AddSingleton<IRoomWordFilterService, RoomWordFilterService>();
    }
}
