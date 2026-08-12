using System.Globalization;
using System.Text;
using Ada.API.DTOs.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

public static class GroupBadgeCodec
{
    public static string Build(IReadOnlyList<GroupBadgePart> parts)
    {
        var badge = new StringBuilder();

        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];

            badge.Append(i == 0 ? 'b' : 's');
            badge.Append(part.PartId.ToString("D3", CultureInfo.InvariantCulture));
            badge.Append(part.ColorId.ToString("D2", CultureInfo.InvariantCulture));
            badge.Append(part.Position.ToString(CultureInfo.InvariantCulture));
        }

        return badge.ToString();
    }
}
