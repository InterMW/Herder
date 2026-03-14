using Domain;
using Infrastructure.Redis;
using MelbergFramework.Core.Time;

namespace DomainService;

public interface IPacketIngestorService
{
    public Task RecordPacket(string serialNumber, string frame);
}

public partial class PacketIngestorService(IPlaneRepository planeRepository, IClock clock) : IPacketIngestorService
{
    public async Task RecordPacket(string serialNumber, string frame)
    {
        var now = (long)(clock.GetUtcNow()-DateTime.UnixEpoch).TotalSeconds;
        var frameBytes = Enumerable
                .Range(0,frame.Length/2)
                .Select(_ => (byte)(byte.Parse(frame.Substring(2*_,2),System.Globalization.NumberStyles.HexNumber))).ToArray();

        var messageType = (byte) (frameBytes[0]>>3);

        var icao = await ExtractIcao(messageType, serialNumber, frameBytes);

        if (string.IsNullOrEmpty(icao))
        {
            return;
        }

        if (!await planeRepository.IsNewMessage(frame))
        {
            //don't record duplicates
            return;
        }

        await planeRepository.RecordPacket(frame, icao, (long)now);
        await planeRepository.MarkIcaoForMoment(icao,(long)now);
    }

    private async Task<string> ExtractIcao(byte messageType, string serialNumber, byte[] frameBytes)
    {
        var now = DateTime.UtcNow;
        string icao;
        //Console.WriteLine(messageType.ToString("x2"));
        switch (messageType)
        {
            case 11:
            case 17:
            case 18:
                icao = ( (frameBytes[1]<<16) | (frameBytes[2] << 8) | (frameBytes[3])).ToString("X6");
                 await planeRepository.RememberIcao(serialNumber,icao);
                break;
            default:
                icao = await bruteForceAP(messageType, serialNumber, frameBytes);
                break;
        }

        return icao;

    }

    public async Task<string> bruteForceAP(byte msgtype, string serialNumber, byte[] frameBytes)
    {
        // int msgbits = mm->msgbits;
        if (msgtype == 0 ||         /* Short air surveillance */
            msgtype == 4 ||         /* Surveillance, altitude reply */
            msgtype == 5 ||         /* Surveillance, identity reply */
            msgtype == 16 ||        /* Long Air-Air survillance */
            msgtype == 20 ||        /* Comm-A, altitude request */
            msgtype == 21 ||        /* Comm-A, identity request */
            msgtype == 24)          /* Comm-C ELM */
        {
            int lastbyte = frameBytes.Length  - 1;

            /* Work on a copy. */
            // memcpy(aux, msg, msgbits / 8);

            /* Compute the CRC of the message and XOR it with the AP field
             * so that we recover the address, because:
             *
             * (ADDR xor CRC) xor CRC = ADDR. */
            UInt32 crc = 0;
            int offset = ((frameBytes.Length*8)  == 112) ? 0 : (112 - 56);
            int j;


            for (j = 0; j < frameBytes.Length * 8; j++)
            {
                int byteIndex = j / 8;
                int bit = j % 8;
                int bitmask = 1 << (7 - bit);

                /* If bit is set, xor with corresponding table entry. */
                if ((frameBytes[byteIndex] & bitmask) != 0)
                    crc ^= CyclicRedundencyCheck.CrcTable[j + offset];
            }

            uint aa1 = frameBytes[lastbyte - 2] ^ (crc >> 16) & 0xff;
            uint aa2 = frameBytes[lastbyte - 1] ^ (crc >> 8) & 0xff;
            uint aa3 = frameBytes[lastbyte] ^ (crc) & 0xff;

            // /* If the obtained address exists in our cache we consider
            //  * the message valid. */
            var Addr = ((aa1 << 16) | (aa2 << 8) | aa3).ToString("X6");
            //Console.WriteLine(Addr);

            if (await planeRepository.ConfirmIcao(serialNumber, Addr))
            {
                return Addr;
            }
            return string.Empty;
        }
        return string.Empty;
    }
    UInt32 modesSChecksum(string frame)
    {
        UInt32 crc = 0;
        int offset = (frame.Length * 4 == 112) ? 0 : (112 - 56);
        int j;

        var valArray =
            Enumerable
                .Range(0, frame.Length / 2)
                .Select(_ => ushort.Parse(frame.Substring(_, 2), System.Globalization.NumberStyles.HexNumber))
                .ToArray();

        for (j = 0; j < frame.Length * 8; j++)
        {
            int byteIndex = j / 8;
            int bit = j % 8;
            int bitmask = 1 << (7 - bit);

            /* If bit is set, xor with corresponding table entry. */
            if ((valArray[byteIndex] & bitmask) != 0)
                crc ^= CyclicRedundencyCheck.CrcTable[j + offset];
        }
        return crc; /* 24 bit checksum. */
    }
    private static int decodeAC13Field(string frame)
    {
        byte m_bit = (byte)(GetByte(frame, 3) & 1 << 6);
        byte q_bit = (byte)(GetByte(frame, 3) & 1 << 4);

        if (m_bit == 0)
        {
            // *unit = MODES_UNIT_FEET;
            if (q_bit != 0)
            {
                /* N is the 11 bit integer resulting from the removal of bit
                 * Q and M */
                int n = ((GetByte(frame,2) & 31) << 6) |
                        ((GetByte(frame,3) & 0x80) >> 2) |
                        ((GetByte(frame,3) & 0x20) >> 1) |
                         (GetByte(frame,3) & 15);
                /* The final altitude is due to the resulting number multiplied
                 * by 25, minus 1000. */
                return n * 25 - 1000;
            }
            else
            {
                /* TODO: Implement altitude where Q=0 and M=0 */
            }
        }
        else
        {
            // *unit = MODES_UNIT_METERS;
            /* TODO: Implement altitude when meter unit is selected. */
        }
        return -1;
    }
    // 0 1 2 3 4 5 6 7 8 9 A B C D E F
    // 3 5
    // X X X y y y y y 

    private static byte GetByte(string frame, int num)
    {
        return (byte)(byte.Parse(frame.Substring(2 * num)));
    }
}
