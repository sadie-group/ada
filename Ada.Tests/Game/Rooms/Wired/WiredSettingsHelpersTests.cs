using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Db.Models.Constants;
using Ada.Game.Rooms.Wired;
using Moq;

namespace Ada.Tests.Game.Rooms.Wired;

[TestFixture]
public class WiredSettingsHelpersTests
{
    private static readonly ServerRoomConstants _constants = new()
    {
        WiredMaxFurnitureSelection = 5,
        MaxChatMessageLength = 10
    };

    private static IWordFilterService PassthroughFilter()
    {
        var filter = new Mock<IWordFilterService>();

        filter.Setup(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()))
            .Returns((string text, WordFilterContext _) =>
                new WordFilterResultDto { OriginalText = text, FilteredText = text });

        return filter.Object;
    }

    private static PlayerFurnitureItemPlacementDataDto Item(int id) => new()
    {
        Id = id,
        PlayerFurnitureItemId = id,
        PlayerFurnitureItem = new PlayerFurnitureItemDto
        {
            Id = id,
            PlayerId = 1,
            FurnitureItemId = 0,
            LimitedData = "",
            MetaData = "",
            FurnitureItem = new global::Ada.API.DTOs.Furniture.FurnitureItemDto
            {
                Name = "", AssetName = "", InteractionType = ""
            }
        }
    };

    private static PlayerFurnitureItemWiredDataDto Build(int delay, string message = "hi", int selected = 1) =>
        WiredSettingsHelpers.Build(
            Item(1),
            Enumerable.Range(100, selected).Select(Item),
            message,
            [delay],
            delay,
            _constants,
            PassthroughFilter());

    [TestCase(int.MaxValue)]
    [TestCase(5_000_000)]
    [TestCase(121)]
    public void Build_DelayAboveCap_IsClamped(int delay) => Assert.That(Build(delay).Delay, Is.EqualTo(WiredSettingsHelpers.MaxDelayInPulses));

    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void Build_NegativeDelay_IsClampedToZero(int delay) => Assert.That(Build(delay).Delay, Is.Zero);

    [Test]
    public void Build_DelayWithinRange_IsKept() => Assert.That(Build(7).Delay, Is.EqualTo(7));

    [Test]
    public void Build_MoreItemsThanTheAdvertisedCap_IsTruncated() => Assert.That(Build(0, selected: 50).SelectedItems, Has.Count.EqualTo(_constants.WiredMaxFurnitureSelection));

    [Test]
    public void Build_LongMessage_IsTruncatedToTheChatLimit() => Assert.That(Build(0, new string('x', 500)).Message, Has.Length.EqualTo(_constants.MaxChatMessageLength));

    [Test]
    public void Build_Message_GoesThroughTheWordFilter()
    {
        var filter = new Mock<IWordFilterService>();

        filter.Setup(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()))
            .Returns(new WordFilterResultDto { OriginalText = "rude", FilteredText = "****" });

        var data = WiredSettingsHelpers.Build(
            Item(1), [], "rude", [0], 0, _constants, filter.Object);

        Assert.That(data.Message, Is.EqualTo("****"));
    }
}
