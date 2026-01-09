using Domain;
using Infrastructure.Redis;
using MelbergFramework.Core.Time;
namespace DomainService;

public interface IPacketRecorderService
{
    public Task RecordPacket(string serialNumber, string frame);
}

public partial class PacketRecorderService(IPlaneRepository planeRepository, IClock clock) : IPacketRecorderService
{
    public async Task RecordPacket(string serialNumber, string frame)
    {
        var result = new ModeSMessage(frame);
        result.MessageType = result.GetDownlinkFormat();
        result.MessageBits = result.ModesMessageLenByType(result.MessageType);
        result.Crc = result.GetCrc();
        var crc2 = result.ModesSChecksum();

        result.metype = result.GetBits(4 * 8, 5);/* Extended squitter message type. */
        result.mesub = result.GetBits(37, 3);/* Extended squitter message type. */

        if (!await ConfirmIcao(serialNumber, result))
        {
            return;
        }

        await planeRepository.RecordPacket(frame, result.Addr.ToString("x"),
                (long)Math.Floor((clock.GetUtcNow() - DateTime.UnixEpoch).TotalSeconds));
    }
    private async Task<bool> ConfirmIcao(string serialNumber, ModeSMessage message)
    {
        switch (message.MessageType)
        {
            case 11:
            case 17:
                message.Addr = (uint)((message.GetBits(8, 8) << 16) | (message.GetBits(8 * 2, 8) << 8) | message.GetBits(8 * 3, 8));
                await planeRepository.RememberIcao(serialNumber, message.Addr.ToString("x"));
                return true;
            default:
                return await bruteForceAP(serialNumber, message);
        }
    }
    public async Task<bool> bruteForceAP(string serialNumber, ModeSMessage message)
    {
        // int msgtype = mm->msgtype;
        // int msgbits = mm->msgbits;
        int msgtype = message.MessageType;
        if (msgtype == 0 ||         /* Short air surveillance */
            msgtype == 4 ||         /* Surveillance, altitude reply */
            msgtype == 5 ||         /* Surveillance, identity reply */
            msgtype == 16 ||        /* Long Air-Air survillance */
            msgtype == 20 ||        /* Comm-A, altitude request */
            msgtype == 21 ||        /* Comm-A, identity request */
            msgtype == 24)          /* Comm-C ELM */
        {
            UInt32 crc;
            int lastbyte = (message.MessageBits / 8) - 1;

            /* Work on a copy. */
            // memcpy(aux, msg, msgbits / 8);

            /* Compute the CRC of the message and XOR it with the AP field
             * so that we recover the address, because:
             *
             * (ADDR xor CRC) xor CRC = ADDR. */
            crc = message.ModesSChecksum();
            uint aa1 = (ushort)(message.GetBits(8 * (lastbyte - 2), 8) ^ (crc >> 16) & 0xff);
            uint aa2 = (ushort)(message.GetBits(8 * (lastbyte - 1), 8) ^ (crc >> 8) & 0xff);
            uint aa3 = (ushort)(message.GetBits(8 * lastbyte, 8) ^ (crc & 0xff));
            // aux[lastbyte] ^= crc & 0xff;
            // aux[lastbyte - 1] ^= (crc >> 8) & 0xff;
            // aux[lastbyte - 2] ^= (crc >> 16) & 0xff;

            // /* If the obtained address exists in our cache we consider
            //  * the message valid. */
            message.Addr = (aa1 << 16) | (aa2 << 8) | aa3;

            if (await planeRepository.ConfirmIcao(serialNumber, message.Addr))
            {
                await planeRepository.RememberIcao(serialNumber, message.Addr);
                return true;
            }
            return false;
        }
        return false;
    }
}
