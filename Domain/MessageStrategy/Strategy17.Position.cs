namespace Domain.MessageStrategy;

public partial class Strategy17
{
    static void DecodeESAirbornePosition(Plane plane, int[] binary)
    {
        // Airborne position and altitude
        var oe = binary[53];
        if (oe == 0)
        {
            plane.OddPositionMessage = binary;
        }
        else
        {
            plane.EvenPositionMessage = binary;
        }

        plane.PositionTimestamp[oe] = plane.LastUpdate;

        (float, float) latlon = (-1, -1);

        if (false && plane.TPos != 0 && (plane.LastUpdate - plane.TPos) < 180)
        {
            var rlat = plane.Latitude.Value;
            var rlon = plane.Longitude.Value;

            latlon = position_with_ref(binary, rlat, rlon);
        }
        else
        if (plane.PositionTimestamp[0] != 0 && plane.PositionTimestamp[1] != 0
                && Math.Abs(plane.PositionTimestamp[0] - plane.PositionTimestamp[1]) < 10)
        {
            latlon = position(
                    plane.OddPositionMessage,
                    plane.EvenPositionMessage,
                    plane.PositionTimestamp[0],
                    plane.PositionTimestamp[1]
                    );
        }

        if (latlon is not (-1, -1))
        {
            plane.TPos = plane.LastUpdate;
            plane.Longitude = latlon.Item2;
            plane.Latitude = latlon.Item1;
        }

        int AC12Field = PlaneFrameDecoder.ExtractValue(binary, 40, 12);

        if (AC12Field != 0)
        {
            // Only attempt to decode if a valid (non zero) altitude is present
            var altitude = PlaneFrameDecoder.decodeAC12Field(AC12Field);
            if (altitude >= 0)
            {
                plane.Altitude = altitude;
            }
        }
    }

    static (float, float) position(int[] msg0, int[] msg1, long t0, long t1)
    {
        var tc0 = typecode(msg0);
        var tc1 = typecode(msg1);

        if (9 <= tc0 && tc0 <= 18 && 9 <= tc1 && tc1 <= 18)
        {
            return airborne_position(msg0, msg1, t0, t1);
        }

        if (20 <= tc0 && tc0 <= 22 && 20 <= tc1 && tc1 <= 22)
        {
            return airborne_position(msg0, msg1, t0, t1);
        }

        return (-1, -1);
    }

    static int typecode(int[] frameBin) => PlaneFrameDecoder.ExtractValue(frameBin, 32, 5);

    static (float, float) airborne_position(int[] mb0, int[] mb1, long t0, long t1)
    {
        var cprlat_even = PlaneFrameDecoder.ExtractValue(mb0, 54, 17);
        var cprlon_even = PlaneFrameDecoder.ExtractValue(mb0, 71, 17);
        var cprlat_odd = PlaneFrameDecoder.ExtractValue(mb1, 54, 17);
        var cprlon_odd = PlaneFrameDecoder.ExtractValue(mb1, 71, 17);
        var (a, b, c) = Cpr.decodeCPRairborne(cprlat_even, cprlon_even, cprlat_odd, cprlon_odd, t0 < t1);
        if (a == 0)
        {
            return ((float)b, (float)c);
        }

        return (-1, -1);
    }

    private static (float, float) position_with_ref(int[] frameBin, float lat_ref, float lon_ref)
    {
        var tc = PlaneFrameDecoder.ExtractValue(frameBin, 32, 5);
        (float, float) result = (-1, -1);
        if (5 <= tc && tc <= 8)
        {
            result = surface_position_with_ref(frameBin, lat_ref, lon_ref);
        }

        if ((9 <= tc && tc <= 18) || (20 <= tc && tc <= 22))
        {
            result = airborne_position_with_ref(frameBin, lat_ref, lon_ref);
        }

        return result;
    }

    public static (float, float) airborne_position_with_ref(int[] frameBin, float lat_ref, float lon_ref)
    {
        var cprlat = PlaneFrameDecoder.ExtractValue(frameBin, 54, 17) / 131072.0;
        var cprlon = PlaneFrameDecoder.ExtractValue(frameBin, 71, 17) / 131072.0;
        var i = frameBin[53];
        var d_lat = i == 1 ? 360 / 59 : 360 / 60;
        var j = Math.Floor(0.5 + lat_ref / d_lat - cprlat);
        var lat = d_lat * (j + cprlat);
        var ni = Cpr.cprNFunction(lat, i == 1);

        float d_lon;
        if (ni > 0)
        {
            d_lon = 360 / ni;
        }
        else
        {
            d_lon = 360;
        }
        var m = (int)Math.Floor(0.5 + lon_ref / d_lon - cprlon);
        var lon = d_lon * (m + cprlon);
        return ((float)lat, (float)lon);
    }

    private static (float, float) surface_position_with_ref(int[] frameBin, float lat_ref, float lon_ref)
    {
        var cprlat = PlaneFrameDecoder.ExtractValue(frameBin, 54, 17) / 131072.0;
        var cprlon = PlaneFrameDecoder.ExtractValue(frameBin, 71, 17) / 131072.0;

        var i = frameBin[53];

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

        return ((float)lat, (float)lon);
    }

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

}
