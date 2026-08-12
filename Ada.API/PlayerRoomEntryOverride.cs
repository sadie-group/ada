using System.Drawing;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.API;

public sealed record PlayerRoomEntryOverride(int RoomId, Point Point, HDirection Direction);
