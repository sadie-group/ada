using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Networking.Events.Handlers.Groups;
using Ada.Tests.Common;
using Ada.Networking.Events.Handlers.Groups.Forums;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Groups;

[TestFixture]
public class ForumPostMessageEventHandlerTests
{
    private const int GuildId = 5;
    private const long PlayerId = 1;

    private Mock<IGroupForumRepository> _forum = null!;
    private Mock<IWordFilterService> _filter = null!;
    private INetworkClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _forum = new Mock<IGroupForumRepository>();
        _forum.Setup(x => x.PostThreadAsync(GuildId, PlayerId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((global::Ada.API.DTOs.Groups.ForumThreadDto?)null);

        _filter = new Mock<IWordFilterService>();
        Passthrough();

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(TestPlayers.Minimal(PlayerId, "poster"));

        var client = new Mock<INetworkClient>();
        client.SetupGet(x => x.Player).Returns(player.Object);
        _client = client.Object;
    }

    private void Passthrough() =>
        _filter.Setup(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()))
            .Returns((string text, WordFilterContext _) =>
                new WordFilterResultDto { OriginalText = text, FilteredText = text });

    private ForumPostMessageEventHandler Handler(string subject, string message)
    {
        var group = new global::Ada.API.DTOs.GroupDto { Id = GuildId, PlayerId = PlayerId, Name = "g", Description = "" };

        var groups = new Mock<IGroupRepository>();
        groups.Setup(x => x.GetByIdAsync(GuildId)).ReturnsAsync(group);
        groups.Setup(x => x.GetMembershipAsync(GuildId, PlayerId)).ReturnsAsync((global::Ada.API.DTOs.Groups.GroupMembershipDto?)null);

        return new ForumPostMessageEventHandler(groups.Object, _forum.Object, _filter.Object)
        {
            GuildId = GuildId,
            ThreadId = 0,
            Subject = subject,
            Message = message
        };
    }

    [Test]
    public async Task Post_FiltersBothSubjectAndBody()
    {
        _filter.Setup(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()))
            .Returns(new WordFilterResultDto { OriginalText = "rude", FilteredText = "****" });

        await Handler("rude", "rude").HandleAsync(_client);

        _forum.Verify(x => x.PostThreadAsync(GuildId, PlayerId, "****", "****"), Times.Once);
    }

    [Test]
    public async Task Post_BlockedContent_IsNotStored()
    {
        _filter.Setup(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()))
            .Returns(new WordFilterResultDto
            {
                OriginalText = "bad",
                FilteredText = "bad",
                Action = WordFilterAction.Block
            });

        await Handler("bad", "bad").HandleAsync(_client);

        _forum.Verify(
            x => x.PostThreadAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Post_OverlongContent_IsTruncated()
    {
        await Handler(new string('s', 5_000), new string('m', 50_000)).HandleAsync(_client);

        _forum.Verify(x => x.PostThreadAsync(
            GuildId,
            PlayerId,
            It.Is<string>(v => v.Length == GroupTextLimits.MaxForumSubjectLength),
            It.Is<string>(v => v.Length == GroupTextLimits.MaxForumMessageLength)), Times.Once);
    }
}
