namespace Domain;

public static partial class PlaneFrameDecoder
{
    public static uint MODES_NON_ICAO_ADDRESS = 1 << 24;
    static void decodeExtendedSquitter(Plane plane, ModeSMessage mm)
    {
        var metype = GetValue(GetBin(mm.Frame).Skip(32).Take(5));
        mm.ME = mm.Frame.Substring(4 * 2, 7 * 2);
        var check_imf = 0;

        // Check CF on DF18 to work out the format of the ES and whether we need to look for an IMF bit
        if (mm.MessageType == 18)
        {
            mm.CF = GetBits(mm.Frame, 5, 7);
            switch (mm.CF)
            {
                case 0: // ADS-B Message from a non-transponder device, AA field holds 24-bit ICAO aircraft address
                    mm.addrtype = AddrType.ADDR_ADSB_ICAO_NT;
                    break;

                case 1: // Reserved for ADS-B Message in which the AA field holds anonymous address or ground vehicle address or fixed obstruction address
                    mm.addrtype = AddrType.ADDR_ADSB_OTHER;
                    mm.addr |= MODES_NON_ICAO_ADDRESS;
                    break;

                case 2: // Fine TIS-B Message
                        // IMF=0: AA field contains the 24-bit ICAO aircraft address
                        // IMF=1: AA field contains the 12-bit Mode A code followed by a 12-bit track file number
                    mm.source = DataSource.SOURCE_TISB;
                    mm.addrtype = AddrType.ADDR_TISB_ICAO;
                    check_imf = 1;
                    break;

                case 3: //   Coarse TIS-B airborne position and velocity.
                        // IMF=0: AA field contains the 24-bit ICAO aircraft address
                        // IMF=1: AA field contains the 12-bit Mode A code followed by a 12-bit track file number

                    // For now we only look at the IMF bit.
                    mm.source = DataSource.SOURCE_TISB;
                    mm.addrtype = AddrType.ADDR_TISB_ICAO;
                    //if (GetBits(me, 1)) THIS IS NOT TESTED
                    //   setIMF(mm);
                    return;

                case 5: // Fine TIS-B Message, AA field contains a non-ICAO 24-bit address
                    mm.addrtype = AddrType.ADDR_TISB_OTHER;
                    mm.source = DataSource.SOURCE_TISB;
                    mm.addr |= MODES_NON_ICAO_ADDRESS;
                    break;

                case 6: // Rebroadcast of ADS-B Message from an alternate data link
                        // IMF=0: AA field holds 24-bit ICAO aircraft address
                        // IMF=1: AA field holds anonymous address or ground vehicle address or fixed obstruction address
                    mm.addrtype = AddrType.ADDR_ADSR_ICAO;
                    check_imf = 1;
                    break;

                default:    // All others, we don't know the format.
                    mm.addrtype = AddrType.ADDR_UNKNOWN;
                    mm.addr |= MODES_NON_ICAO_ADDRESS; // assume non-ICAO
                    return;
            }

            return;
        }

        switch (metype)
        {
            case 1:
            case 2:
            case 3:
            case 4:
                decodeESIdentAndCategory(plane, mm);
                break;

            case 19:
                decodeESAirborneVelocity(plane,mm.Frame);
                break;

            case 5:
            case 6:
            case 7:
            case 8:
                //decodeESSurfacePosition(mm, check_imf);
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
            case 20:
            case 21:
            case 22: // Airborne position, GNSS altitude (HAE or MSL)
                decodeESAirbornePosition(plane, mm, check_imf);
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

    static void decodeESAirborneVelocity(Plane plane, string frame)
    {

        Console.WriteLine("velo");

        var mb = GetBin(frame).Skip(32);

        var subtype = GetValue(mb.Skip(5).Take(3));

        var velEW = GetValue(mb.Skip(14).Take(10));
        var velNS = GetValue(mb.Skip(25).Take(10));
        Console.WriteLine(( velEW, velNS));
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

            trk = trk + trk < 0 ? 360 : 0;

            plane.Speed = spd;
            plane.Track = (int)trk;
        }
    }
    //         trk_or_hdg = trk

    //     spd_type = "GS"
    //     dir_type = "TRUE_NORTH"

    // else:
    //     if mb[13] == "0":
    //         hdg = None
    //     else:
    //         hdg = common.bin2int(mb[14:24]) / 1024 * 360.0

    //     trk_or_hdg = hdg

    //     spd = common.bin2int(mb[25:35])
    //     spd = None if spd == 0 else spd - 1
    //     if subtype == 4 and spd is not None:  # Supersonic
    //         spd *= 4

    //     if mb[24] == "0":
    //         spd_type = "IAS"
    //     else:
    //         spd_type = "TAS"

    //     dir_type = "MAGNETIC_NORTH"

    // vr_source = "GNSS" if mb[35] == "0" else "BARO"
    // vr_sign = -1 if mb[36] == "1" else 1
    // vr = common.bin2int(mb[37:46])
    // vs = None if vr == 0 else int(vr_sign * (vr - 1) * 64)

    // if source:
    //     return spd, trk_or_hdg, vs, spd_type, dir_type, vr_source
    // else:
    //     return spd, trk_or_hdg, vs, spd_type
    // }


    static void decodeESIdentAndCategory(Plane plane, ModeSMessage mm)
    {

        // Aircraft Identification and Category

        //mm.mesub = GetBits(mm.ME, 6, 8);

        var bits = GetBin(mm.Frame);

        mm.aircraft_type = mm.metype - 1;
        mm.flight[0] = ais_charset[GetValue(bits.Skip(40).Take(6))];
        mm.flight[1] = ais_charset[GetValue(bits.Skip(46).Take(6))];
        mm.flight[2] = ais_charset[GetValue(bits.Skip(52).Take(6))];
        mm.flight[3] = ais_charset[GetValue(bits.Skip(58).Take(6))];
        mm.flight[4] = ais_charset[GetValue(bits.Skip(64).Take(6))];
        mm.flight[5] = ais_charset[GetValue(bits.Skip(70).Take(6))];
        mm.flight[6] = ais_charset[GetValue(bits.Skip(76).Take(6))];
        mm.flight[7] = ais_charset[GetValue(bits.Skip(82).Take(6))];

        // A common failure mode seems to be to intermittently send
        // all zeros. Catch that here.

        plane.Flight = string.Join("", mm.flight);
        mm.category = ((0x0E - mm.metype) << 4) | mm.mesub;
        mm.callsign_valid = true;//(strcmp(mm.callsign, "@@@@@@@@") != 0);
    }


    static string ais_charset = "@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_ !\"#$%&'()*+,-./0123456789:;<=>?";
    static void decodeESAirbornePosition(Plane plane, ModeSMessage mm, int check_imf)
    {
        // Airborne position and altitude

        // if (check_imf && GetBits(me, 8))
        // setIMF(mm);

        int AC12Field = GetValue(GetBin(mm.Frame).Skip(40).Take(12));


        if (AC12Field != 0)
        {// Only attempt to decode if a valid (non zero) altitude is present
            plane.Altitude = decodeAC12Field(AC12Field, mm.altitude_unit);
            if (mm.altitude != INVALID_ALTITUDE)
            {
                mm.altitude_valid = true;
            }

            mm.altitude_source = (mm.metype == 20 || mm.metype == 21 || mm.metype == 22) ? AltitudeSource.ALTITUDE_GNSS : AltitudeSource.ALTITUDE_BARO;
        }
    }

    static int decodeAC12Field(int AC12Field, AltitudeUnit unit)
    {
        int q_bit = AC12Field & 0x10; // Bit 48 = Q

        unit = AltitudeUnit.UNIT_FEET;
        if (q_bit != 0)
        {
            /// N is the 11 bit integer resulting from the removal of bit Q at bit 4
            int n = ((AC12Field & 0x0FE0) >> 1) |
                     (AC12Field & 0x000F);
            // The final altitude is the resulting number multiplied by 25, minus 1000.
            return ((n * 25) - 1000);
        }
        else
        {
            // Make N a 13 bit Gillham coded altitude by inserting M=0 at bit 6
            int n = ((AC12Field & 0x0FC0) << 1) |
                     (AC12Field & 0x003F);
            n = ModeAToModeC(decodeID13Field(n));
            if (n < -12)
            {
                return INVALID_ALTITUDE;
            }

            return (100 * n);
        }
    }


    // Handle setting a non-ICAO address
    static void setIMF(ModeSMessage mm)
    {
        mm.addr |= MODES_NON_ICAO_ADDRESS;
        switch (mm.addrtype)
        {
            case AddrType.ADDR_ADSB_ICAO:
            case AddrType.ADDR_ADSB_ICAO_NT:
                // Shouldn't happen, but let's try to handle it
                mm.addrtype = AddrType.ADDR_ADSB_OTHER;
                break;

            case AddrType.ADDR_TISB_ICAO:
                mm.addrtype = AddrType.ADDR_TISB_TRACKFILE;
                break;

            case AddrType.ADDR_ADSR_ICAO:
                mm.addrtype = AddrType.ADDR_ADSR_OTHER;
                break;

            default:
                // Nothing.
                break;
        }
    }
}
