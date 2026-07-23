using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.Groups;

namespace Ada.Game.Groups;

public static class GroupServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGroupRepository, GroupRepository>();
        serviceCollection.AddSingleton<IGroupForumRepository, GroupForumRepository>();
    }
}
