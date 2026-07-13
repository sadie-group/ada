using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.Locale;

namespace Ada.Game.Locale;

public static class LocaleServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ILocaleService, LocaleService>();
    }
}