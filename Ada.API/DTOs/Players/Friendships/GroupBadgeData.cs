using Ada.API.Interfaces.Networking.Events.Dtos;

namespace Ada.API.DTOs.Players.Friendships;

public class GroupBadgeData : IGroupBadgeData
{
    public int GroupId { get; set; }
    public required string Badge { get; set; }
}
