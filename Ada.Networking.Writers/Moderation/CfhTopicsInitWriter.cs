using Ada.API;
using Ada.API.DTOs.Moderation;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.CfhTopicsInit)]
public class CfhTopicsInitWriter : AbstractPacketWriter
{
    public required IReadOnlyList<ModerationCfhTopicDto> Topics { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        var categories = Topics
            .GroupBy(x => x.CategoryName)
            .OrderBy(x => x.Min(t => t.Order))
            .ToList();

        writer.WriteInteger(categories.Count);

        foreach (var category in categories)
        {
            writer.WriteString(category.Key);

            var topics = category.OrderBy(x => x.Order).ToList();

            writer.WriteInteger(topics.Count);

            foreach (var topic in topics)
            {
                writer.WriteString(topic.Name);
                writer.WriteInteger(topic.Id);
            }
        }
    }
}
