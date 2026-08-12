using Ada.API.Interfaces.Game.Guides;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Guides;

public static class GuideServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGuideSessionService, GuideSessionService>();
    }
}
