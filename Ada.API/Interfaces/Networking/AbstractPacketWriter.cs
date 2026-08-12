namespace Ada.API.Interfaces.Networking;

public abstract class AbstractPacketWriter
{
    public virtual void OnSerialize(INetworkPacketWriter writer)
    {
    }
}
