using Domain;
using Infrastructure.Redis;
using MelbergFramework.Core.Time;
namespace DomainService;

public interface IPacketDecoderService
{
    public Task DecodePackets(long time);
}

public partial class PacketDecoderService(IPlaneRepository planeRepository, IClock clock) : IPacketDecoderService
{
    public static int INVALID_ALTITUDE = -9999;
    //
    public async Task DecodePackets(long time)
    {
        string icao;
        while(!String.IsNullOrEmpty(icao = await planeRepository.GetNextIcao(time)))
        {
            await HandleIcaoMoment(icao, time);
        }
    }

    private async Task HandleIcaoMoment(string icao, long time)
    {
        var plane = planeRepository.GetLastSeen(icao);
        await foreach(var nextPacket in planeRepository.GetPackets(icao, time))
        {
            

        }
    }
    private async Task DecodePacket(string frame)
    {
        var seconds = (long)Math.Floor((clock.GetUtcNow() - DateTime.UnixEpoch).TotalSeconds);

        // get the planes for that node
        var knownPlanes = await planeRepository.GetValidIcaos(serialNumber);

        var result = new ModeSMessage(frame);
        result.MessageType = result.GetDownlinkFormat();
        result.MessageBits = result.ModesMessageLenByType(result.MessageType);
        result.Crc = result.GetCrc();
        var crc2 = result.ModesSChecksum();

        result.metype = result.GetBits(4 * 8, 5);/* Extended squitter message type. */
        result.mesub = result.GetBits(37, 3);/* Extended squitter message type. */

        if(!await ConfirmIcao(serialNumber, result))
        {
            return;
        }

        switch (result.MessageType)
        {
            /* Decode 13 bit altitude for DF0, DF4, DF16, DF20 */
            case 0 or 4 or 16 or 20:
                result.Altitude = decodeAC13Field(result.GetBits(19, 13));
                break;
            case 17:
                HandleMessageType17(result);
                break;

        }
        //Translate message here instaed
        var message = PlaneFrameDecoder.DecodeModesFrame(frame, knownPlanes.ToHashSet());

        if (message.CrcOk)
        {
            await planeRepository.AddIcao(serialNumber, message.Addr);
        }

        switch (message.MessageType)
        {
            case 0:
                await planeRepository.SetAttribute(serialNumber, message.Addr, seconds, "altitude",
                        $"{message.Altitude}");
            case 17:
                switch (message.metype)
                {
                    case 9:

                }
                break;
        }

        //Apply it to current record of plane for timestamp per node
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
    static int decodeAC13Field(int AC13Field)
    {
        int m_bit = AC13Field & 0x0040; // set = meters, clear = feet
        int q_bit = AC13Field & 0x0010; // set = 25 ft encoding, clear = Gillham Mode C encoding

        if (m_bit == 0)
        {
            if (q_bit != 0)
            {
                // N is the 11 bit integer resulting from the removal of bit Q and M
                int n = ((AC13Field & 0x1F80) >> 2) |
                        ((AC13Field & 0x0020) >> 1) |
                         (AC13Field & 0x000F);
                // The final altitude is resulting number multiplied by 25, minus 1000.
                return ((n * 25) - 1000);
            }
            else
            {
                // N is an 11 bit Gillham coded altitude
                int n = ModeAToModeC(decodeID13Field(AC13Field));
                if (n < -12)
                {
                    return INVALID_ALTITUDE;
                }

                return (100 * n);
            }
        }
        else
        {
            // TODO: Implement altitude when meter unit is selected
            return INVALID_ALTITUDE;
        }
    }
    public static int ModeAToModeC(int ModeA)
    {
        int FiveHundreds = 0;
        int OneHundreds = 0;

        if ((ModeA & 0xFFFF8889) != 0 ||         // check zero bits are zero, D1 set is illegal
            (ModeA & 0x000000F0) == 0)
        { // C1,,C4 cannot be Zero
            return INVALID_ALTITUDE;
        }

        if ((ModeA & 0x0010) != 0) { OneHundreds ^= 0x007; } // C1
        if ((ModeA & 0x0020) != 0) { OneHundreds ^= 0x003; } // C2
        if ((ModeA & 0x0040) != 0) { OneHundreds ^= 0x001; } // C4

        // Remove 7s from OneHundreds (Make 7->5, snd 5->7). 
        if ((OneHundreds & 5) == 5) { OneHundreds ^= 2; }

        // Check for invalid codes, only 1 to 5 are valid 
        if (OneHundreds > 5)
        {
            return INVALID_ALTITUDE;
        }

        //if (ModeA & 0x0001) {FiveHundreds ^= 0x1FF;} // D1 never used for altitude
        if ((ModeA & 0x0002) != 0) { FiveHundreds ^= 0x0FF; } // D2
        if ((ModeA & 0x0004) != 0) { FiveHundreds ^= 0x07F; } // D4

        if ((ModeA & 0x1000) != 0) { FiveHundreds ^= 0x03F; } // A1
        if ((ModeA & 0x2000) != 0) { FiveHundreds ^= 0x01F; } // A2
        if ((ModeA & 0x4000) != 0) { FiveHundreds ^= 0x00F; } // A4

        if ((ModeA & 0x0100) != 0) { FiveHundreds ^= 0x007; } // B1 
        if ((ModeA & 0x0200) != 0) { FiveHundreds ^= 0x003; } // B2
        if ((ModeA & 0x0400) != 0) { FiveHundreds ^= 0x001; } // B4

        // Correct order of OneHundreds. 
        if ((FiveHundreds & 1) != 0) { OneHundreds = 6 - OneHundreds; }

        return ((FiveHundreds * 5) + OneHundreds - 13);
    }
    public static void ExtractPlaneName(ModeSMessage result)
    {
        result.aircraft_type = result.metype - 1;
        result.flight[0] = ModeSMessage.AIS[result.GetBits(40, 6)];
        result.flight[1] = ModeSMessage.AIS[result.GetBits(46, 6)];
        result.flight[2] = ModeSMessage.AIS[result.GetBits(52, 6)];
        result.flight[3] = ModeSMessage.AIS[result.GetBits(58, 6)];
        result.flight[4] = ModeSMessage.AIS[result.GetBits(64, 6)];
        result.flight[5] = ModeSMessage.AIS[result.GetBits(70, 6)];
        result.flight[6] = ModeSMessage.AIS[result.GetBits(76, 6)];
        result.flight[7] = ModeSMessage.AIS[result.GetBits(82, 6)];
        result.flight[8] = '\0';
    }
    private static void HandleMessageType17(ModeSMessage result)
    {
        switch (result.metype)
        {
            case >= 1 and <= 4: ExtractPlaneName(result); break;
            case >= 9 and <= 18: IsolateAltitudeLatLon(result); break;
            case 19:
                switch (result.mesub)
                {
                    case 1 or 2: AirborneVelocity(result); break;
                    case 3 or 4: Heading(result); break;
                };
                break;

            default:
                break;
        };

    }
    private static void AirborneVelocity(ModeSMessage result)
    {
        result.ew_dir = result.GetBit(45);
        result.ew_velocity = result.GetBits(46, 10);//((msg[5] & 3) << 8) | msg[6];
        result.ns_dir = result.GetBit(56);//(msg[7] & 0x80) >> 7;
        result.ns_velocity = result.GetBits(57, 10);//((msg[7] & 0x7f) << 3) | ((msg[8] & 0xe0) >> 5);
        result.vert_rate_source = result.GetBit(67);//(msg[8] & 0x10) >> 4;
        result.vert_rate_sign = result.GetBit(68);//(msg[8] & 0x8) >> 3;
        result.vert_rate = result.GetBits(69, 9);// ((msg[8] & 7) << 6) | ((msg[9] & 0xfc) >> 2);
        /* Compute velocity and angle from the two speed
         * components. */
        result.velocity = (int)Math.Sqrt(result.ns_velocity * result.ns_velocity +
                            result.ew_velocity * result.ew_velocity);
        if (result.velocity != 0)
        {
            int ewv = result.ew_velocity;
            int nsv = result.ns_velocity;
            double heading;

            if (result.ew_dir != 0) ewv *= -1;
            if (result.ns_dir != 0) nsv *= -1;
            heading = Math.Atan2(ewv, nsv);

            /* Convert to degrees. */
            result.heading = (int)(heading * 360 / (Math.PI * 2));
            /* We don't want negative values but a 0-360 scale. */
            if (result.heading < 0) result.heading += 360;
        }
        else
        {
            result.heading = 0;
        }
    }

    private static void IsolateAltitudeLatLon(ModeSMessage result)
    {
        /* Airborne position Message */
        // result.fflag = msg[6] & (1 << 2);
        // result.tflag = msg[6] & (1 << 3);
        var cpr = result.GetBit(53);
        result.Altitude = result.decodeAC12Field();
        result.raw_latitude = result.GetBits(54, 17);

        // ((msg[6] & 3) << 15) | (msg[7] << 7) | (msg[8] >> 1);

        result.raw_longitude = result.GetBits(71, 17);
        // ((msg[8] & 1) << 16) | (msg[9] << 8) | msg[10];
    }
    private static void Heading(ModeSMessage result)
    {
        result.heading_is_valid = result.GetBit(45);//msg[5] & (1 << 2);
        result.heading = (int)((360.0 / 128) * result.GetBits(46, 10));
    }

    private async Task<bool> ConfirmIcao(string serialNumber, ModeSMessage message)
    {
        switch (message.MessageType)
        {
            case 11:
            case 17:
                message.Addr = (uint)((message.GetBits(8, 8) << 16) | (message.GetBits(8 * 2, 8) << 8) | message.GetBits(8 * 3, 8));
                await planeRepository.RememberIcao(serialNumber, message.Addr);
                return true;
            default:
                return await bruteForceAP(serialNumber, message);
        }
    }
    static int decodeID13Field(int ID13Field)
    {
        int hexGillham = 0;

        if ((ID13Field & 0x1000) != 0) { hexGillham |= 0x0010; } // Bit 12 = C1
        if ((ID13Field & 0x0800) != 0) { hexGillham |= 0x1000; } // Bit 11 = A1
        if ((ID13Field & 0x0400) != 0) { hexGillham |= 0x0020; } // Bit 10 = C2
        if ((ID13Field & 0x0200) != 0) { hexGillham |= 0x2000; } // Bit  9 = A2
        if ((ID13Field & 0x0100) != 0) { hexGillham |= 0x0040; } // Bit  8 = C4
        if ((ID13Field & 0x0080) != 0) { hexGillham |= 0x4000; } // Bit  7 = A4
                                                                 //if (ID13Field & 0x0040) {hexGillham |= 0x0800;} // Bit  6 = X  or M 
        if ((ID13Field & 0x0020) != 0) { hexGillham |= 0x0100; } // Bit  5 = B1 
        if ((ID13Field & 0x0010) != 0) { hexGillham |= 0x0001; } // Bit  4 = D1 or Q
        if ((ID13Field & 0x0008) != 0) { hexGillham |= 0x0200; } // Bit  3 = B2
        if ((ID13Field & 0x0004) != 0) { hexGillham |= 0x0002; } // Bit  2 = D2
        if ((ID13Field & 0x0002) != 0) { hexGillham |= 0x0400; } // Bit  1 = B4
        if ((ID13Field & 0x0001) != 0) { hexGillham |= 0x0004; } // Bit  0 = D4

        return (hexGillham);
    }
}
