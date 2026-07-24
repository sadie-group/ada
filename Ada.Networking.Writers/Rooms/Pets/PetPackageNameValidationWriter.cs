using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetPackageNameValidation)]
public class PetPackageNameValidationWriter : AbstractPacketWriter
{
    public const int CloseWidget = 0;
    public const int NameTooLong = 1;
    public const int NameTooShort = 2;
    public const int ContainsInvalidChars = 3;
    public const int Unacceptable = 4;

    public required int ItemId { get; init; }
    public required int ErrorCode { get; init; }
    public required string ErrorText { get; init; }
}
