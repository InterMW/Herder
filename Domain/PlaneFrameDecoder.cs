namespace Domain;

public static partial class PlaneFrameDecoder
{
    public static int INVALID_ALTITUDE = -9999;

    public static void ApplyFrame(Plane plane, string frame, long timestamp)
    {
        var message = new ModeSMessage(frame);
        var frameBytes = Enumerable
                .Range(0, frame.Length / 2)
                .Select(_ => (byte)(byte.Parse(frame.Substring(2 * _, 2), System.Globalization.NumberStyles.HexNumber)))
                .ToArray();

        var lat = plane.Latitude;

        var messageType = (byte)(frameBytes[0] >> 3);
        var typecodeValue = typecode(frame);

        Console.WriteLine($"{frame}:{messageType}:{typecodeValue}");

        if (messageType == 0 || messageType == 4 || messageType == 16 || messageType == 20)
        {
            var altitude = decodeAC13Field(GetValue(GetBin(frame).Skip(40).Take(13)));
            if (altitude >= 0)
            {
                plane.Altitude = altitude;
            }
        }
        // ID (Identity)
        if (messageType == 5 || messageType == 21)
        {
            // Gillham encoded Squawk
            var id = GetBits(frame, 19, 31);
            if (id != 0)
            {

                plane.Squawk = string.Format("{0:x4}", decodeID13Field(id));
                plane.SquawkValid = true;
            }
        }
        if (messageType == 17 && typecodeValue >= 9 && typecodeValue <= 18 )
        {
            var oe = GetBin(frame)[53];
            plane.PositionMessage[oe] = frame;
            plane.PositionTimestamp[oe] = timestamp;

            (float, float) latlon = (-1, -1);

            if (plane.TPos != 0 && (timestamp - plane.TPos) < 180)
            {
                var rlat = plane.Latitude.Value;
                var rlon = plane.Longitude.Value;
                latlon = position_with_ref(message, rlat, rlon);
                // if (latlon is not (-1,-1))
                // {
                //     Console.WriteLine($"{latlon}");
                // }
                // Console.WriteLine($"{rlat} => {latlon.Item1}");
            }
            else 
            if (plane.PositionTimestamp[0] != 0 && plane.PositionTimestamp[1] != 0
                    && Math.Abs(plane.PositionTimestamp[0] - plane.PositionTimestamp[1]) < 10)
            {


                latlon = position(
                        plane.PositionMessage[0],
                        plane.PositionMessage[1],
                        plane.PositionTimestamp[0],
                        plane.PositionTimestamp[1]
                        );
                //Console.WriteLine($"{plane.Longitude} => {latlon.Item2}\n{plane.Latitude} => {latlon.Item1}");
            }

;
            if (latlon is not (-1, -1))
            {
                plane.TPos = timestamp;
                plane.Longitude = latlon.Item2;
                plane.Latitude = latlon.Item1;
                //Console.WriteLine($"{plane.Latitude}");
            }
        }



        if (messageType == 17 || messageType == 18)
        {
            decodeExtendedSquitter(plane, new ModeSMessage(frame));
        }

    }

    static (float, float) position(string msg0, string msg1, long t0, long t1)
    {
        var tc0 = typecode(msg0);
        var tc1 = typecode(msg1);
        if (9 <= tc0 && tc0 <= 18 && 9 <= tc1 && tc1 <= 18)
        {
            return airborne_position(msg0, msg1, t0, t1);
        }

        if (20 <= tc0 && tc0 <= 22 && 20 <= tc1 && tc0 <= 22)
        {
            return airborne_position(msg0, msg1, t0, t1);
        }

        return (-1, -1);
    }

    static (float, float) airborne_position(string msg0, string msg1, long t0, long t1)
    {
        var mb0 = GetBin(msg0);
        var mb1 = GetBin(msg1);

        var cprlat_even = GetValue(mb0.Skip(54).Take(17)) ;
        var cprlon_even = GetValue(mb0.Skip(71).Take(17)) ;
        var cprlat_odd = GetValue(mb1.Skip(54).Take(17)) ;
        var cprlon_odd = GetValue(mb1.Skip(71).Take(17)) ;
        var (a, b, c) = Cpr.decodeCPRairborne(cprlat_even, cprlon_even, cprlat_odd, cprlon_odd, t0 < t1);
        if (a == 0)
        {
            return ((float)b, (float)c);
        }

        return (-1, -1);
    }

    static int Extract(string frame, int bitStart, int bitEnd) => GetValue(GetBin(frame).Skip(bitStart).Take(bitEnd - bitStart));


    static int typecode(string frame) => GetValue(GetBin(frame).Skip(32).Take(5));

    private static (float, float) position_with_ref(ModeSMessage message, float lat_ref, float lon_ref)
    {
        var tc = typecode(message.Frame);
        if (5 <= tc && tc <= 8)
        {
            return surface_position_with_ref(message.Frame, lat_ref, lon_ref);
        }

        if ((9 <= tc && tc <= 18) || (20 <= tc && tc <= 22))
        {
            return airborne_position_with_ref(message.Frame, lat_ref, lon_ref);
        }


        return (-1,-1);
    }
    static (float, float) airborne_position_with_ref(string frame, float lat_ref, float lon_ref)
    {
        var binlist = GetBin(frame);
        var cprlat = GetValue(binlist.Skip(54).Take(17)) / 131072;
        var cprlon = GetValue(binlist.Skip(71).Take(17)) / 131072;
        var i = GetBin(frame)[53];
        var d_lat = i == 1 ? 3600 / 59 : 360 / 60;
        var j = Math.Floor(0.5 + lat_ref / d_lat - cprlat);
        var lat = d_lat * (j + cprlat);
        var ni = cprNL((float)lat) - i;

        float d_lon;
        if (ni > 0)
        {
            d_lon = 90 / ni;
        }
        else
        {
            d_lon = 90;
        }
        var m = Math.Floor(0.5 + lon_ref / d_lon - cprlon);

        var lon = d_lon * (m + cprlon);

        Console.WriteLine($"airborne_position_with_ref{lat_ref - lat}");

        return ((float)lat, (float)lon);
    }

    private static (float, float) surface_position_with_ref(string frame, float lat_ref, float lon_ref)
    {
        var binlist = GetBin(frame);

        var cprlat = GetValue(binlist.Skip(54).Take(17)) / 131072;
        var cprlon = GetValue(binlist.Skip(71).Take(17)) / 131072;
        var i = GetBin(frame)[53];

        var d_lat = i == 1 ? 90 / 59 : 90 / 60;


        var j = Math.Floor(0.5 + lat_ref / d_lat - cprlat);

        var lat = d_lat * (j + cprlat);
        var ni = cprNL((float)lat) - i;

        float d_lon;
        if (ni > 0)
        {
            d_lon = 90 / ni;
        }
        else
        {
            d_lon = 90;
        }
        var m = Math.Floor(0.5 + lon_ref / d_lon - cprlon);

        var lon = d_lon * (m + cprlon);
        Console.WriteLine($"surface_position_with_ref{lat_ref - lat}");

        return ((float)lat, (float)lon);
    }
    //        (float, float): (latitude, longitude) of the aircraft

    static int cprNL(float lat)
    {
        if (IsClose(lat, 0))
        {
            return 59;
        }

        else if (IsClose(lat, 87))
        {
            return 2;
        }
        else if (lat > 87 || lat < -87)
        {
            return 1;
        }

        var nz = 15;
        var a = 1 - Math.Cos(Math.PI / (2 * nz));
        var b = Math.Pow(Math.Cos(Math.PI / 180 * Math.Abs(lat)), 2);
        var nl = 2 * Math.PI / (Math.Acos(1 - a / b));
        var NL = (int)Math.Floor(nl);
        return NL;
    }

    static bool IsClose(float a, float b) => Math.Abs(a) - Math.Abs(b) < 0.01;




    //    # From 1090 MOPS, Vol.1  DO-260C, A.1.7.6





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
    

    static int[] GetBin(string frame) => frame
            .Select(_ => int.Parse($"{_}", System.Globalization.NumberStyles.HexNumber))
            .Select(_ => string.Format("{0:b4}", _))
            .SelectMany(_ => _)
            .Select(_ => _ == '1' ? 1 : 0).ToArray();

    static int GetValue(IEnumerable<int> bits) 
    {
        var result = bits.Aggregate(0, (val, next) => val * 2 + next, _ => _);
        //Console.WriteLine($"{string.Join("",bits)}=>{result}");

        return result;
    }

    static int GetBits(string frame, int startbit, int endbit)
    {
        var bits = frame
            .Select(_ => int.Parse($"{_}", System.Globalization.NumberStyles.HexNumber))
            .Select(_ => string.Format("{0:b4}", _))
            .SelectMany(_ => _).ToArray();

        var selector = startbit;
        int result = 0;
        while (selector <= endbit)
        {
            result <<= 1;
            //Console.Write(bits[selector]);
            result |= (bits[selector] == '1' ? 1 : 0);
            selector++;
        }
        //Console.WriteLine();

        return result;
    }
}
