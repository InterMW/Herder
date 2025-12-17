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
        var something = (thing1 << 16) ^ (thing2 << 8) ^ thing3;

        Console.WriteLine(something.ToString("x"));
        this.CorrectedBits = 0;
        this.Addr = 0;

        // Console.WriteLine("Downlink format = " + downlink);
        Console.WriteLine("\n7C1B28 please");

        switch (this.MessageType)
        {
            case 0: // short air-air surveillance
            case 4: // surveillance, altitude reply
            case 5: // surveillance, altitude reply
            case 16: //long air-air sruveillance
            case 24: //Comm-D (ELM)
            case 25: //Comm-D (ELM)
            case 26: //Comm-D (ELM)
            case 27: //Comm-D (ELM)
            case 28: //Comm-D (ELM)
            case 29: //Comm-D (ELM)
            case 30: //Comm-D (ELM)
            case 31: //Comm-D (ELM)


            default:
                return;
        }

    }

    public UInt32 Identity =>
        ((GetByteMasked(3, 0x80) >> 5) | (GetByteMasked(2, 0x02) >> 0) | (GetByteMasked(2, 0x08) >> 3)) * 1000 +
        ((GetByteMasked(3, 0x02) << 1) | (GetByteMasked(3, 0x08) >> 2) | (GetByteMasked(3, 0x20) >> 5)) * 100 +
        ((GetByteMasked(2, 0x01) << 2) | (GetByteMasked(2, 0x04) >> 1) | (GetByteMasked(2, 0x10) >> 4)) * 10 +
        ((GetByteMasked(3, 0x01) << 2) | (GetByteMasked(3, 0x04) >> 1) | (GetByteMasked(3, 0x10) >> 4));

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

    private UInt32 GetByteMasked(int byteIndex, int mask) => (UInt32)(GetBits(byteIndex * 8, 8) & mask);


    private int GetBit(int index)
    {
        if (index < 64)
        {
            return (int)(Part1 >>> (63 - index)) & 1;
        }

        return 0;
    }

    private int GetBits(int start, int length)
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

    public int Address() => GetBits(8, 24);

    public int ModesMessageLenByType(int type) => (type & 0x10) != 0 ? ModesLongMsgBits : ModesShortMsgBits;

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


    public UInt16 aa1;
    public UInt16 aa2;
    public UInt16 aa3;
    public int MessageType;
    public int MessageBits;
    public Int64 Part1 { get; }
    public Int64 Part2 { get; }
    bool AltitudeValid;
    public uint Altitude { get; set; }
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
    private int Addr;

}
