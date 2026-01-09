namespace Domain;

public class ModeSMessage
{
    public ModeSMessage(string value)
    {
        Console.WriteLine(value);
        if (value.Length == 14)
        {
            Part1 = (Int64.Parse(value.Substring(0, 14), System.Globalization.NumberStyles.HexNumber)) << (64 - 56);
        }
        else
        {
            Part1 = Int64.Parse(value.Substring(0, 16));
            // Part2 =  Int64.Parse(value.Substring(17, ));
        }


        foreach (var index in Enumerable.Range(0, 64))
        {
            // Console.Write((Part1 >> index) & 1);
            Console.Write(GetBit(index));
        }

        Console.WriteLine();
        foreach (var index in Enumerable.Range(0, 64))
        {
            Console.Write(GetBits(index, 1));
        }


        Console.WriteLine();

        this.MessageType = GetDownlinkFormat();
        this.MessageBits = ModesMessageLenByType(this.MessageType);
        Console.WriteLine($"I am going to turn A to {Int32.Parse("A", System.Globalization.NumberStyles.HexNumber)}");
        var things = Splitty(value).Select(_ => Int32.Parse($"{_}", System.Globalization.NumberStyles.HexNumber))
                .ToArray();
        // this.Crc = ModesSChecksum();
        var thing1 = (UInt32)GetBits(8, 8);
        Console.WriteLine(thing1.ToString("x"));
        var thing2 = (UInt32)GetBits(8 * 2, 8);
        Console.WriteLine(thing2.ToString("x"));
        var thing3 = (UInt32)GetBits(8 * 3, 8);
        Console.WriteLine(thing3.ToString("x"));
        // rem = rem ^ (message[n-3] << 16) ^ (message[n-2] << 8) ^ (message[n-1]);
        this.Addr = (thing1 << 16) ^ (thing2 << 8) ^ thing3;

        Console.WriteLine(this.Addr.ToString("x"));
        this.CorrectedBits = 0;

        // Console.WriteLine("Downlink format = " + downlink);


    }

    public UInt32 Identity =>
        ((GetByteMasked(3, 0x80) >> 5) | (GetByteMasked(2, 0x02) >> 0) | (GetByteMasked(2, 0x08) >> 3)) * 1000 +
        ((GetByteMasked(3, 0x02) << 1) | (GetByteMasked(3, 0x08) >> 2) | (GetByteMasked(3, 0x20) >> 5)) * 100 +
        ((GetByteMasked(2, 0x01) << 2) | (GetByteMasked(2, 0x04) >> 1) | (GetByteMasked(2, 0x10) >> 4)) * 10 +
        ((GetByteMasked(3, 0x01) << 2) | (GetByteMasked(3, 0x04) >> 1) | (GetByteMasked(3, 0x10) >> 4));
    /* Decode the 12 bit AC altitude field (in DF 17 and others).
 * Returns the altitude or 0 if it can't be decoded. */
    public int decodeAC12Field()
    {
        int q_bit = GetBit(5 * 8 + 7);//msg[5] & 1;

        if (q_bit != 0)
        {
            /* N is the 11 bit integer resulting from the removal of bit
             * Q */
            // *unit = MODES_UNIT_FEET;
            // x x x x 4 5 6 X 
            int n = GetBits(5 * 8 + 4, 3) << 4 | GetBits(6 * 8, 4);
            // int n = ((msg[5] >> 1) << 4) | ((msg[6] & 0xF0) >> 4);
            /* The final altitude is due to the resulting number multiplied
             * by 25, minus 1000. */
            return n * 25 - 1000;
        }
        else
        {
            return 0;
        }
    }

    private IEnumerable<string> Splitty(string input)
    {
        char last = ' ';
        foreach (var character in input)
        {
            if (last == ' ')
            {
                last = character;
            }
            else
            {
                // Console.WriteLine(string.Join("",last,character));
                yield return string.Join("", last, character);
                last = ' ';
            }
        }
    }

    private void ShortAirToAir()
    {
        this.VerticalStatus = GetBit(5);

        this.CrosslinkCapable = GetBit(6);

    }

    public UInt32 GetCrc()
    {
        var offset = this.MessageBits / 8;
        var thing1 = (UInt32)GetBits((offset - 3) * 8, 8);
        var thing2 = (UInt32)GetBits((offset - 2) * 8, 8);
        var thing3 = (UInt32)GetBits((offset - 1) * 8, 8);

        return (thing1 << 16) | (thing2 << 8) | thing3;
    }


    public int GetDownlinkFormat() => GetBits(0, 5);

    public UInt32 GetByteMasked(int byteIndex, int mask) => (UInt32)(GetBits(byteIndex * 8, 8) & mask);


    public int GetBit(int index)
    {
        if (index < 64)
        {
            return (int)(Part1 >>> (63 - index)) & 1;
        }

        return 0;
    }

    public int GetBits(int start, int length)
    {
        int result = 0;
        int pointer = start;

        while (length > 0)
        {
            result = result << 1;
            var bitt = GetBit(pointer);
            result |= GetBit(pointer);
            pointer++;
            length--;
        }
        return result;
    }

    public UInt32 ModesSChecksum()
    {
        UInt32 crc = 0;
        int offset = (this.MessageBits == 112) ? 0 : 56;
        int n = this.MessageBits / 8;

        for (int i = 0; i < this.MessageBits; ++i)
        {
            if (GetBits(i, 1) != 0)
            {
                crc ^= CyclicRedundencyCheck.CrcTable[i + offset];
            }
        }
        return crc; /* 24 bit checksum. */
    }

    public static char[] AIS => "?ABCDEFGHIJKLMNOPQRSTUVWXYZ????? ???????????????0123456789??????".ToCharArray();

    public Source SourceType { get; set; }


    public int ModesMessageLenByType(int type) => (type & 0x10) != 0 ? ModesLongMsgBits : ModesShortMsgBits;
    public bool bruteForceAP(HashSet<uint> validIcaos)
    {
        // int msgtype = mm->msgtype;
        // int msgbits = mm->msgbits;
        int msgtype = this.MessageType;
        if (msgtype == 0 ||         /* Short air surveillance */
            msgtype == 4 ||         /* Surveillance, altitude reply */
            msgtype == 5 ||         /* Surveillance, identity reply */
            msgtype == 16 ||        /* Long Air-Air survillance */
            msgtype == 20 ||        /* Comm-A, altitude request */
            msgtype == 21 ||        /* Comm-A, identity request */
            msgtype == 24)          /* Comm-C ELM */
        {
            UInt32 addr;
            UInt32 crc;
            int lastbyte = (this.MessageBits / 8) - 1;

            /* Work on a copy. */
            // memcpy(aux, msg, msgbits / 8);

            /* Compute the CRC of the message and XOR it with the AP field
             * so that we recover the address, because:
             *
             * (ADDR xor CRC) xor CRC = ADDR. */
            // crc = modesChecksum(aux, msgbits);
            crc = this.GetCrc();
            // aux[lastbyte] ^= crc & 0xff;
            // aux[lastbyte - 1] ^= (crc >> 8) & 0xff;
            // aux[lastbyte - 2] ^= (crc >> 16) & 0xff;

            // /* If the obtained address exists in our cache we consider
            //  * the message valid. */
            // addr = aux[lastbyte] | (aux[lastbyte - 1] << 8) | (aux[lastbyte - 2] << 16);
            if (validIcaos.Contains(crc))
            {
                this.aa1 = (ushort)(GetBits(8 * (lastbyte - 2), 8) ^ (crc >> 16) & 0xff);
                this.aa2 = (ushort)(GetBits(8 * (lastbyte - 1), 8) ^ (crc >> 8) & 0xff);
                this.aa3 = (ushort)(GetBits(8 * lastbyte, 8) ^ (crc & 0xff));
                this.Addr = (this.aa1 << 16) | (this.aa2 << 8) | this.aa3;
                return true;
            }
        }
        return false;
    }

    public uint decodeAC13Field()
    {
        uint m_bit = GetByteMasked(3, 0x2);
        uint q_bit = GetByteMasked(3, 0x8);

        if (m_bit == 0)
        {
            // *unit = MODES_UNIT_FEET;
            if (q_bit != 0)
            {
                /* N is the 11 bit integer resulting from the removal of bit
                 * Q and M */
                uint n = (GetByteMasked(2, 31) << 6) |
                        (GetByteMasked(3, 0x80) >> 2) |
                        (GetByteMasked(3, 0x20) >> 1) |
                        (GetByteMasked(3, 15));
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
        return 0;
    }


    public uint aa1;
    public uint aa2;
    public uint aa3;
    public int ca;              /* Responder capabilities. */

    /* DF 17 */
    public int metype;                 /* Extended squitter message type. */
    public int mesub;                  /* Extended squitter message subtype. */
    public int heading_is_valid;
    public int heading;
    public int aircraft_type;
    public int fs;
    public int dr;
    public int um;
    public int fflag;                  /* 1 = Odd, 0 = Even CPR message. */
    public int tflag;                  /* UTC synchronized? */
    public int raw_latitude;           /* Non decoded latitude */
    public int raw_longitude;          /* Non decoded longitude */
    public char[] flight = new char[9];             /* 8 chars flight number. */
    public int ew_dir;                 /* 0 = East, 1 = West. */
    public int ew_velocity;            /* E/W velocity. */
    public int ns_dir;                 /* 0 = North, 1 = South. */
    public int ns_velocity;            /* N/S velocity. */
    public int vert_rate_source;       /* Vertical rate source. */
    public int vert_rate_sign;         /* Vertical rate sign. */
    public int vert_rate;              /* Vertical rate. */
    public int velocity;               /* Computed from EW and NS velocity. */

    public int MessageType;
    public int MessageBits;
    public Int64 Part1 { get; }
    public Int64 Part2 { get; }
    bool AltitudeValid;
    public int Altitude { get; set; }
    public int VerticalStatus { get; private set; }
    public int CrosslinkCapable { get; private set; }
    public UInt32 Crc;
    public bool CrcOk;
    public int ErrorBit;

    public static int ModesLongMsgBytes = 14;
    public static int ModesLongMsgBits = ModesLongMsgBytes * 8;
    public static int ModesShortMsgBytes = 7;
    public static int ModesShortMsgBits = ModesShortMsgBytes * 8;

    private int CorrectedBits;
    public uint Addr { get; set; }

}
