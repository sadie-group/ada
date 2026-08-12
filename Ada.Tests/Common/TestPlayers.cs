using Ada.API.DTOs.Players;

namespace Ada.Tests.Common;

public static class TestPlayers
{
    public static PlayerDto Minimal(long id, string username) => new(
        id,
        username,
        "",
        DateTimeOffset.UtcNow,
        [],
        new PlayerDataDto(),
        new PlayerAvatarDataDto(),
        [], [], [], [],
        new PlayerNavigatorSettingsDto(),
        new PlayerGameSettingsDto(),
        [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], []);
}
