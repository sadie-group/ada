using Ada.API.Interfaces.Game.Groups;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Groups;

public static class GroupServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGroupRepository, GroupRepository>();
        serviceCollection.AddSingleton<IGroupForumRepository, GroupForumRepository>();
    }
}
