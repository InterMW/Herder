namespace Domain;

public static class PlaneFrameDecoder
{
    public static int INVALID_ALTITUDE = -9999;
    public static ModeSMessage DecodeModesFrame(string frame, HashSet<UInt32> validIcaos)
    {
        Console.WriteLine(frame);
        var result = new ModeSMessage(frame);

        result.MessageType = result.GetDownlinkFormat();
        Console.WriteLine($"The message type is {result.MessageType}");
        result.MessageBits = result.ModesMessageLenByType(result.MessageType);
        Console.WriteLine($"The messageBits is {result.MessageBits}");
        result.Crc = result.GetCrc();
        Console.WriteLine($"The crc is {result.Crc.ToString("x")}");
        var crc2 = result.ModesSChecksum();
        result.CrcOk = crc2 == result.Crc;
        Console.WriteLine($"crc2 is {crc2.ToString("x")}");


        /* Note that most of the other computation happens *after* we fix
         * the single bit errors, otherwise we would need to recompute the
         * fields again. */
        // result.CA = result.GetByteMasked(0, 7);/* Responder capabilities. */

        /* ICAO address */
        result.aa1 = (UInt16)result.GetBits(1 * 8, 8);
        result.aa2 = (UInt16)result.GetBits(2 * 8, 8);
        result.aa3 = (UInt16)result.GetBits(3 * 8, 8);

        /* DF 17 type (assuming this is a DF17, otherwise not used) */
        result.metype = result.GetBits(4 * 8, 5);/* Extended squitter message type. */
        result.mesub = result.GetBits(37, 3);/* Extended squitter message type. */

        /* Fields for DF4,5,20,21 */
        result.fs = result.GetBits(5, 3);/* Flight status for DF4,5,20,21 */
        result.dr = result.GetBits(8, 5);/* Request extraction of downlink request. */
        result.um = result.GetBits(13, 6);/* Request extraction of downlink request. */

        /* In the squawk (identity) field bits are interleaved like that
         * (message bit 20 to bit 32):
         *
         * C1-A1-C2-A2-C4-A4-ZERO-B1-D1-B2-D2-B4-D4
         *
         * So every group of three bits A, B, C, D represent an integer
         * from 0 to 7.
         *
         * The actual meaning is just 4 octal numbers, but we convert it
         * into a base ten number tha happens to represent the four
         * octal numbers.
         *
        //  * For more info: http://en.wikipedia.org/wiki/Gillham_code */
        // {
        //     int a, b, c, d;

        //     a = ((msg[3] & 0x80) >> 5) |
        //         ((msg[2] & 0x02) >> 0) |
        //         ((msg[2] & 0x08) >> 3);
        //     b = ((msg[3] & 0x02) << 1) |
        //         ((msg[3] & 0x08) >> 2) |
        //         ((msg[3] & 0x20) >> 5);
        //     c = ((msg[2] & 0x01) << 2) |
        //         ((msg[2] & 0x04) >> 1) |
        //         ((msg[2] & 0x10) >> 4);
        //     d = ((msg[3] & 0x01) << 2) |
        //         ((msg[3] & 0x04) >> 1) |
        //         ((msg[3] & 0x10) >> 4);
        //     mm->identity = a * 1000 + b * 100 + c * 10 + d;
        // }

        /* DF 11 & 17: try to populate our ICAO addresses whitelist.
         * DFs with an AP field (xored addr and crc), try to decode it. */
        if (result.MessageType != 11 && result.MessageType != 17)
        {
            /* Check if we can check the checksum for the Downlink Formats where
             * the checksum is xored with the aircraft ICAO address. We try to
             * brute force it using a list of recently seen aircraft addresses. */

            if (result.bruteForceAP(validIcaos))
            {
                /* We recovered the message, mark the checksum as valid. */
                result.CrcOk = true;
            }
            else
            {
                result.CrcOk = false;
            }
        }
        else
        {
            /* If this is DF 11 or DF 17 and the checksum was ok,
             * we can add this address to the list of recently seen
             * addresses. */
            // if (mm->crcok && mm->errorbit == -1)
            // {
           result.Addr = (result.aa1 << 16) | (result.aa2 << 8) | result.aa3;
          // addRecentlySeenICAOAddr(addr);
            // }
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

        /* Decode extended squitter specific stuff. */
        // result.phase_corrected = 0; /* Set to 1 by the caller if needed. */
        return result;
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
    private static void Heading(ModeSMessage result)
    {
        result.heading_is_valid = result.GetBit(45);//msg[5] & (1 << 2);
        result.heading = (int)((360.0 / 128) * result.GetBits(46, 10));
    }

    private static void IsolateAltitudeLatLon(ModeSMessage result)
    {
        /* Airborne position Message */
        // result.fflag = msg[6] & (1 << 2);
        // result.tflag = msg[6] & (1 << 3);
        result.Altitude = result.decodeAC12Field();
        result.raw_latitude = result.GetBits(54, 17);

        // ((msg[6] & 3) << 15) | (msg[7] << 7) | (msg[8] >> 1);
        result.raw_longitude = result.GetBits(71, 17);
        // ((msg[8] & 1) << 16) | (msg[9] << 8) | msg[10];
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
    //=========================================================================
    //
    // Decode the 13 bit AC altitude field (in DF 20 and others).
    // Returns the altitude, and set 'unit' to either UNIT_METERS or UNIT_FEET.
    //
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

    private static ModeSMessage ExtractValues(string frame)
    {
        //turn the frame to bytes (length 4):w
        //
        IEnumerable<byte> values = frame
                                .ToCharArray()
                                .Select(_ => Convert.ToByte($"{_}", 16));


        return null;
    }

    private static DownlinkFormat ExtractDownlinkFormat(Span<byte> bytes)
    {
        int firstHalf = (bytes[0] << 4);
        int intermediate = firstHalf | bytes[1];

        return (DownlinkFormat)(intermediate >> 3);
    }

    // private static bool ExtractVerticalStatus(Span<byte> bytes) => bytes[]


    private static int ExtractTransponderCapacity(Span<byte> bytes)
    {
        // int cutout = bytes[2] & )
        return 0;
    }

    private static int ExtractValue(Span<byte> bytes, int startBit, int numBits)
    {
        int startIndex = startBit / 4;


        //beginning

        return 0;


    }
}
