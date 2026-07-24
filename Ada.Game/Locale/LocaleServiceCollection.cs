using Ada.API.Interfaces.Game.Locale;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Locale;

public static class LocaleServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ILocaleService, LocaleService>();
    }
}