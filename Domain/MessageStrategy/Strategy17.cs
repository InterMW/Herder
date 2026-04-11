namespace Domain.MessageStrategy;

public partial class Strategy17 : IMessageStrategy
{
    public int DownlinkFormat => 17;

    public void Interpret(Plane plane, int[] binary)
    {
        var metype = PlaneFrameDecoder.ExtractValue(binary, 32, 5);

        switch (metype)
        {
            case 1:
            case 2:
            case 3:
            case 4:
                DecodeESIdentAndCategory(plane, binary, metype);
                break;
            case 19:
                DecodeESAirborneVelocity(plane, binary);
                break;
            case 5:
            case 6:
            case 7:
            case 8:
                // DecodeESSurfacePosition(mm, check_imf);
                break;
            // Airborne position, baro altitude only
            case 0:
            // Airborne position, baro
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
                // case 20:
                // case 21:
                // case 22: // Airborne position, GNSS altitude (HAE or MSL)
                DecodeESAirbornePosition(plane, binary);
                break;

            case 23:
                // decodeESTestMessage(mm);
                break;

            case 24: // Reserved for Surface System Status
                break;

            case 28:
                // decodeESAircraftStatus(mm, check_imf);
                break;

            case 29:
                // decodeESTargetStatus(mm, check_imf);
                break;

            case 30: // Aircraft Operational Coordination
                break;

            case 31:
                // decodeESOperationalStatus(mm, check_imf);
                break;

            default:
                break;
        }
    }

    static void DecodeESAirborneVelocity(Plane plane, int[] binary)
    {
        var mb = binary.Skip(32).ToArray();

        var subtype = PlaneFrameDecoder.ExtractValue(mb, 5, 3);

        var velEW = PlaneFrameDecoder.ExtractValue(mb, 14, 10);
        var velNS = PlaneFrameDecoder.ExtractValue(mb, 25, 10);

        if (velEW == 0 || velNS == 0)
            return;


        if (subtype == 1 || subtype == 2)
        {
            var vEW_sign = mb.Skip(13).First() == 1 ? -1 : 1;
            velEW -= 1;
            if (subtype == 2)
            {
                velEW *= 4;
            }
            var vNS_sign = mb.Skip(24).First() == 1 ? -1 : 1;
            velNS -= 1;
            if (subtype == 2)
            {
                velNS *= 4;
            }


            var velSN = vNS_sign * velNS;
            var velWE = vEW_sign * velEW;

            var spd = (int)Math.Sqrt(velSN * velSN + velWE * velWE);

            var trk = Double.RadiansToDegrees(Math.Atan2(velWE, velSN));

            trk = trk + (trk < 0 ? 360 : 0);

            plane.Speed = spd;
            plane.Track = (int)trk;
        }
    }

    static void DecodeESIdentAndCategory(Plane plane, int[] binary, int metype)
    {
        // mm.aircraft_type = mm.metype - 1;
        var flight = string.Join("", Enumerable.Range(0, 8).Select(_ => GetCharSelect(_, binary)));

        // A common failure mode seems to be to intermittently send
        // all zeros. Catch that here.

        if (string.Compare(flight, "@@@@@@@@") != 0)
        {
            plane.Flight = flight;
            var mesub = PlaneFrameDecoder.ExtractValue(binary, 37, 3);
            plane.Category = string.Format("{0:X2}", ((0x0E - metype) << 4) | mesub);
        }
    }

    static string ais_charset = "@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_ !\"#$%&'()*+,-./0123456789:;<=>?";
    static char GetCharSelect(int charNumber, int[] binary) =>
        ais_charset[PlaneFrameDecoder.ExtractValue(binary, 40 + charNumber * 6, 6)];
}
