using Ada.API.DTOs;
using Ada.API.DTOs.Catalog.Pages;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.Networking.Events.Handlers.Catalog;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Catalog;

[TestFixture]
public class CatalogPageAccessTests
{
    private static IPlayerLogic PlayerWithRoles(params int[] roleIds)
    {
        var playerData = new PlayerDto(
            1L,
            "TestUser",
            "test@example.com",
            DateTimeOffset.UtcNow,
            roleIds.Select(x => new RoleDto { Id = x }).ToList(),
            new PlayerDataDto(),
            new PlayerAvatarDataDto(),
            [], [], [], [],
            new PlayerNavigatorSettingsDto(),
            new PlayerGameSettingsDto(),
            [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], []);

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(playerData);

        return player.Object;
    }

    private static CatalogPageDto Page(bool enabled = true, bool visible = true, int? roleId = null)
        => new() { Id = 1, Enabled = enabled, Visible = visible, RoleId = roleId };

    [Test]
    public void CanAccess_NullPage_IsDenied() => Assert.That(CatalogPageAccess.CanAccess(null, PlayerWithRoles()), Is.False);

    [Test]
    public void CanAccess_EnabledVisibleUnrestrictedPage_IsAllowed() => Assert.That(CatalogPageAccess.CanAccess(Page(), PlayerWithRoles()), Is.True);

    [Test]
    public void CanAccess_DisabledPage_IsDenied() => Assert.That(CatalogPageAccess.CanAccess(Page(enabled: false), PlayerWithRoles()), Is.False);

    [Test]
    public void CanAccess_HiddenPage_IsDenied() => Assert.That(CatalogPageAccess.CanAccess(Page(visible: false), PlayerWithRoles()), Is.False);

    [Test]
    public void CanAccess_RoleRestrictedPage_WithoutRole_IsDenied() => Assert.That(CatalogPageAccess.CanAccess(Page(roleId: 7), PlayerWithRoles(1, 2)), Is.False);

    [Test]
    public void CanAccess_RoleRestrictedPage_WithRole_IsAllowed() => Assert.That(CatalogPageAccess.CanAccess(Page(roleId: 7), PlayerWithRoles(1, 7)), Is.True);
}
