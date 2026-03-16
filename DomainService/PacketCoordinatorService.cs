using System.Diagnostics;
using Domain;
using Infrastructure.Redis;

namespace DomainService;

public interface IPacketCoordindatorService
{
    public Task Coordinate(long time);
}

public class PacketCoordindatorService(IPlaneRepository planeRepository): IPacketCoordindatorService
{
    public async Task Coordinate(long time)
    {
        //Find out what icao's have been seen
        
        var count = 0;
        var timer = new Stopwatch();
        timer.Start();

        var result = new List<Plane>();

        await foreach(var seen in planeRepository.GetIcaosForMoment(time))
        {
            //Console.WriteLine($"starting coord of {seen}");
            result.Add(await ProcessAndUpdatePlane(seen, time));
            count ++;
        }

        timer.Stop();

        Console.WriteLine($"I FOUND {result.Count()} in {timer.ElapsedMilliseconds}");
        await planeRepository.SaveFrame(new SkyFrame(){ Timestamp = time, Planes = result.ToArray()});;
    }


    private async Task<Plane> ProcessAndUpdatePlane(string icao, long time)
    {
        var currentRecord = await planeRepository.GetPlane(icao);
        string packet;
        while (!string.IsNullOrEmpty(packet = await planeRepository.GetNextPacket(icao, time)))
        {
            PlaneFrameDecoder.ApplyFrame( currentRecord, packet, time);
        }

        await planeRepository.UpdatePlane(currentRecord);

        return currentRecord;
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
                icao = ((frameBytes[1] << 16) | (frameBytes[2] << 8) | (frameBytes[3])).ToString("X6");
                await planeRepository.RememberIcao(serialNumber, icao);
                //Console.WriteLine($"I saw {icao}");

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
            int lastbyte = frameBytes.Length - 1;

            /* Work on a copy. */
            // memcpy(aux, msg, msgbits / 8);

            /* Compute the CRC of the message and XOR it with the AP field
             * so that we recover the address, because:
             *
             * (ADDR xor CRC) xor CRC = ADDR. */
            UInt32 crc = 0;
            int offset = ((frameBytes.Length * 8) == 112) ? 0 : (112 - 56);
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
            Console.WriteLine(Addr);

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

}
