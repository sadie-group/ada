using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.DTOs.Server;

namespace Ada.API.DTOs.Players;

public record PlayerDto(
    long Id,
    string Username,
    string Email,
    DateTimeOffset CreatedAt,
    List<RoleDto> Roles,
    PlayerDataDto? Data,
    PlayerAvatarDataDto? AvatarData,
    List<PlayerTagDto> Tags,
    ICollection<PlayerRoomLikeDto> RoomLikes,
    ICollection<PlayerRelationshipDto> OriginRelationships,
    ICollection<PlayerRelationshipDto> TargetRelationships,

    PlayerNavigatorSettingsDto? NavigatorSettings,
    PlayerGameSettingsDto? GameSettings,
    ICollection<PlayerBadgeDto> Badges,
    ICollection<PlayerFurnitureItemDto> FurnitureItems,
    ICollection<PlayerWardrobeItemDto> WardrobeItems,
    ICollection<PlayerSubscriptionDto> Subscriptions,
    ICollection<PlayerRespectDto> Respects,
    ICollection<PlayerSavedSearchDto> SavedSearches,
    ICollection<PlayerFriendshipDto> OutgoingFriendships,
    ICollection<PlayerFriendshipDto> IncomingFriendships,
    ICollection<PlayerIgnoreDto> OutgoingIgnores,
    ICollection<PlayerIgnoreDto> IncomingIgnores,
    ICollection<ServerPeriodicCurrencyRewardLogDto> RewardLogs,
    ICollection<RoomDto> Rooms,
    ICollection<GroupDto> Groups,
    ICollection<PlayerBotDto> Bots,
    ICollection<PlayerRoomVisitDto> RoomVisits,
    ICollection<PlayerBanDto> Bans,
    ICollection<PlayerSsoTokenDto> Tokens
);