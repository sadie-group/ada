using Ada.API.Interfaces.Game.Navigator;
using Ada.Game.Navigator.Filterers;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Navigator;

public static class NavigatorServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        serviceCollection.AddTransient<INavigatorRoomProvider, NavigatorRoomProvider>();
        
        serviceCollection.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo<INavigatorSearchFilterer>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime());
    }
}