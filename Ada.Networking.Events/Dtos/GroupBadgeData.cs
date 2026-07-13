using Ada.API.Interfaces.Networking.Events.Dtos;

namespace Ada.Networking.Events.Dtos;

public class GroupBadgeData : IGroupBadgeData
{
    public int GroupId { get; set; }
    public required string Badge { get; set; }
}