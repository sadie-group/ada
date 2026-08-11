using Ada.API.DTOs.Navigator;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Navigator;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Constants;
using Ada.Networking.Writers.Navigator;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorSearch)]
public class NavigatorSearchEventHandler(
    INavigatorTabProvider navigatorTabProvider,
    INavigatorRoomProvider navigatorRoomProvider,
    IRoomRepository roomRepository,
    IPlayerRepository playerRepository)
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

        if ((DateTime.UtcNow - client.Player.State.LastNavigatorSearch).TotalMilliseconds <
            CooldownIntervals.NavigatorSearch)
        {
            return;
        }

        client.Player.State.LastNavigatorSearch = DateTime.UtcNow;

        // Tab layout is operator-edited reference data, so it comes from a cache rather than a
        // fresh query on every message.
        var categories = await navigatorTabProvider.GetCategoriesForTabAsync(TabName);

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
