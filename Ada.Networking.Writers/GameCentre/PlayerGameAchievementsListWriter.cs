using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.GameCentre;

[PacketId(ServerPacketId.GameCentreConfig)]
public class PlayerGameAchievementsListWriter : AbstractPacketWriter;