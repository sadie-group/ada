using Ada.API.DTOs.Navigator;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Navigator;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Navigator;
using Ada.Networking.Writers.Navigator;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorSearch)]
public class NavigatorSearchEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    INavigatorRoomProvider navigatorRoomProvider,
    IRoomRepository roomRepository,
    IPlayerRepository playerRepository,
    IMapper mapper)
    : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public string? TabName { get; set; }
    public string? SearchQuery { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var tab = await dbContext.Set<NavigatorTab>()
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Name == TabName);

        var dbCategories = tab?
            .Categories
            .OrderBy(x => x.OrderId)
            .ToList() ?? [];

        var categories = mapper.Map<List<NavigatorCategoryDto>>(dbCategories);

        var categoryRoomMap = new Dictionary<NavigatorCategoryDto, List<RoomDto>>();

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            categoryRoomMap[new NavigatorCategoryDto
            {
                Name = "Search Results",
                CodeName = "",
                OrderId = 0,
                TabId = 1
            }] = await navigatorRoomProvider.GetRoomsForSearchQueryAsync(SearchQuery);
        }
        else
        {
            foreach (var category in categories)
            {
                categoryRoomMap.Add(category, await navigatorRoomProvider.GetRoomsForCategoryNameAsync(client.Player, category.CodeName));
            }
        }

        var ownerIds = categoryRoomMap.Values
            .SelectMany(x => x)
            .Select(x => x.OwnerId)
            .Distinct()
            .ToList();

        var resolved = await playerRepository.GetPlayerUsernamesByIdsAsync(ownerIds);
        var ownerUsernames = ownerIds.ToDictionary(id => id,
            id => resolved.GetValueOrDefault(id, "Unknown User"));

        var liveUserCounts = categoryRoomMap.Values
            .SelectMany(x => x)
            .Select(x => x.Id)
            .Distinct()
            .Select(id => (Id: id, Room: roomRepository.TryGetRoomById(id)))
            .Where(x => x.Room != null)
            .ToDictionary(x => x.Id, x => x.Room!.UserRepository.Count);

        var searchResultPagesWriter = new NavigatorSearchResultPagesWriter
        {
            TabName = TabName,
            SearchQuery = SearchQuery,
            CategoryRoomMap = categoryRoomMap,
            LiveUserCounts = liveUserCounts,
            OwnerUsernames = ownerUsernames
        };

        await client.WriteToStreamAsync(searchResultPagesWriter);
    }
}
