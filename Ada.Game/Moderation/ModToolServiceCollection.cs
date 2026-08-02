using Ada.API.Interfaces.Game.Moderation;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Moderation;

public static class ModToolServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IModToolRepository, ModToolRepository>();
        serviceCollection.AddSingleton<IModerationTicketService, ModerationTicketService>();
    }
}
