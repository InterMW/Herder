namespace Domain;

public static class PlaneFrameDecoder
{
    public static ModeSMessage DecodeModesFrame(string frame)
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
        Console.WriteLine($"crc2 is {crc2.ToString("x")}");
    
        result.ErrorBit = -1;
        result.CrcOk = result.Crc == crc2;

        if(!result.CrcOk && 
                (result.MessageType == 11 || result.MessageType == 17))
        {


        }

        





        // Console.WriteLine(result.Identity.ToString("x"));
        if(result.MessageType != 11 && result.MessageType != 17)
        {

        }
        else
        {

        }

            /* Decode 13 bit altitude for DF0, DF4, DF16, DF20 */
        if (result.MessageType == 0 || result.MessageType == 4 ||
            result.MessageType == 16 || result.MessageType == 20) {
            result.Altitude = result.decodeAC13Field();
            Console.WriteLine($"My altitude is {result.Altitude}");
        }
            
        return result;

    }

    private static ModeSMessage ExtractValues(string frame)
    {
        //turn the frame to bytes (length 4):w
        //
        IEnumerable<byte> values = frame
                                .ToCharArray()
                                .Select(_ => Convert.ToByte($"{_}",16));


            return null;




    }

    private static DownlinkFormat ExtractDownlinkFormat(Span<byte> bytes)
    {
        int firstHalf = (bytes[0] << 4);
        int intermediate = firstHalf | bytes[1];

        return (DownlinkFormat)(intermediate>>3);
    }

    // private static bool ExtractVerticalStatus(Span<byte> bytes) => bytes[]


    private static int ExtractTransponderCapacity(Span<byte> bytes)
    {
        // int cutout = bytes[2] & )
        return 0;
    }

    private static int ExtractValue(Span<byte> bytes, int startBit, int numBits)
    {
        int startIndex = startBit/4;


        //beginning

    return 0;
        

    }
}
