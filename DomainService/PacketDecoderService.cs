namespace DomainService;

public interface IPacketDecoderService
{
    public Task DecodePacket(string serialNumber, string frame);
}

public class PacketDecoderService : IPacketDecoderService
{
    public Task DecodePacket(string serialNumber, string frame)
    {
        //Hit cache to either confirm or create device
        
        //Translate message
        
        //Apply it to current record of plane for timestamp per node
        throw new NotImplementedException();
    }
}
