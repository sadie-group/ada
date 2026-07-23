using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.WordFilter;

namespace Ada.Game.WordFilter;

public static class WordFilterServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IWordFilterService, WordFilterService>();
    }
}
