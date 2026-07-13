using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Competition;

namespace Ada.Networking.Events.Handlers.Competition;

[PacketId(EventHandlerId.CompetitionTimingCode)]
public class CompetitionTimingCodeEventHandler : INetworkPacketEventHandler
{
    public string? Data { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (Data == null)
        {
            return;
        } 
        
        if (Data.Contains(';'))
        {
            var pieces = Data.Split(";");

            foreach (var piece in pieces)
            {
                if (piece.Contains(','))
                {
                    await client.WriteToStreamAsync(new CompetitionTimingCodeWriter
                    {
                        Schedule = piece,
                        Code = piece.Split(",").Last()
                    });
                }
                else
                {
                    await client.WriteToStreamAsync(new CompetitionTimingCodeWriter
                    {
                        Schedule = Data,
                        Code = piece
                    });
                }
            }
        }
        else
        {
            await client.WriteToStreamAsync(new CompetitionTimingCodeWriter
            {
                Schedule = Data,
                Code = Data.Split(",").Last()
            });
        }
    }
}