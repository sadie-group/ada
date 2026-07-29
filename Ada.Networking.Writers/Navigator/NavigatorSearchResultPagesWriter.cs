using Ada.API;
using Ada.API.DTOs.Navigator;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Navigator;

[PacketId(ServerPacketId.NavigatorRooms)]
public class NavigatorSearchResultPagesWriter : AbstractPacketWriter
{
    public required string? TabName { get; init; }
    public required string? SearchQuery { get; init; }
    public required Dictionary<NavigatorCategoryDto, List<RoomDto>> CategoryRoomMap { get; init; }
    public required IRoomRepository RoomRepository { get; init; }
    public required Dictionary<long, string> OwnerUsernames { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteString(TabName);
        writer.WriteString(SearchQuery);
        
        writer.WriteInteger(CategoryRoomMap.Count);

        foreach (var (category, rooms) in CategoryRoomMap)
        {
            writer.WriteString(category.CodeName);
            writer.WriteString(category.Name);
            writer.WriteInteger(0);
            writer.WriteBool(false);
            writer.WriteInteger(0);

            writer.WriteInteger(rooms.Count);
            
            foreach (var room in rooms)
            {
                var liveRoom = RoomRepository.TryGetRoomById(room.Id);
                var userCount = liveRoom == null ? 0 : liveRoom.UserRepository.Count;
                
                writer.WriteLong(room.Id);
                writer.WriteString(room.Name);
                writer.WriteLong(room.OwnerId);
                writer.WriteString(OwnerUsernames.GetValueOrDefault(room.OwnerId, "Unknown User"));
                writer.WriteInteger((int) room.Settings.AccessType);
                writer.WriteInteger(userCount);
                writer.WriteInteger(room.MaxUsersAllowed);
                writer.WriteString(room.Description);
                writer.WriteInteger(0);
                writer.WriteInteger(room.PlayerLikes.Count);
                writer.WriteInteger(0);
                writer.WriteInteger(1);
                writer.WriteInteger(room.Tags.Count);

                foreach (var tag in room.Tags)
                {
                    writer.WriteString(tag.Name);
                }
                
                writer.WriteInteger((int) RoomBitmask.ShowOwner);
            }
        }
    }
}