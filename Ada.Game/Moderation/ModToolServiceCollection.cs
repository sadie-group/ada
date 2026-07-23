using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.Moderation;

namespace Ada.Game.Moderation;

public static class ModToolServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IModToolRepository, ModToolRepository>();
    }
}
