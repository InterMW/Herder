namespace Domain;

public static partial class PlaneFrameDecoder
{
    public static uint MODES_NON_ICAO_ADDRESS = 1 << 24;
    static void decodeExtendedSquitter(ModeSMessage mm)
    {
        var metype = mm.metype = GetBits(mm.Frame, 0, 4);
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
        }

        switch (metype)
        {
            case 1:
            case 2:
            case 3:
            case 4:
                decodeESIdentAndCategory(mm);
                break;

            case 19:
                //decodeESAirborneVelocity(mm, check_imf);
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
                decodeESAirbornePosition(mm, check_imf);
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
    static void decodeESIdentAndCategory(ModeSMessage mm)
    {
        // Aircraft Identification and Category

        mm.mesub = GetBits(mm.ME, 6, 8);

        mm.aircraft_type = mm.metype - 1;
        mm.flight[0] = ais_charset[GetBits(mm.Frame, 40, 6)];
        mm.flight[1] = ais_charset[GetBits(mm.Frame, 46, 6)];
        mm.flight[2] = ais_charset[GetBits(mm.Frame, 52, 6)];
        mm.flight[3] = ais_charset[GetBits(mm.Frame, 58, 6)];
        mm.flight[4] = ais_charset[GetBits(mm.Frame, 64, 6)];
        mm.flight[5] = ais_charset[GetBits(mm.Frame, 70, 6)];
        mm.flight[6] = ais_charset[GetBits(mm.Frame, 76, 6)];
        mm.flight[7] = ais_charset[GetBits(mm.Frame, 82, 6)];
        mm.flight[8] = '\0';

        // A common failure mode seems to be to intermittently send
        // all zeros. Catch that here.

        mm.category = ((0x0E - mm.metype) << 4) | mm.mesub;
        mm.callsign_valid = true;//(strcmp(mm.callsign, "@@@@@@@@") != 0);
    }

    static string ais_charset = "@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_ !\"#$%&'()*+,-./0123456789:;<=>?";
    static void decodeESAirbornePosition(ModeSMessage mm, int check_imf)
    {
        // Airborne position and altitude

        // if (check_imf && GetBits(me, 8))
        // setIMF(mm);

        int AC12Field = GetBits(mm.ME, 9, 20);

        if (mm.metype == 0)
        {
            mm.cpr_nucp = 0;
        }
        else
        {
            // Catch some common failure modes and don't mark them as valid
            // (so they won't be used for positioning)

            mm.cpr_lat = GetBits(mm.ME, 23, 39);
            mm.cpr_lon = GetBits(mm.ME, 40, 56);

            if (AC12Field == 0 && mm.cpr_lon == 0 && (mm.cpr_lat & 0x0fff) == 0 && mm.metype == 15)
            {
                // Seen from at least:
                //   400F3F (Eurocopter ECC155 B1) - Bristow Helicopters
                //   4008F3 (BAE ATP) - Atlantic Airlines
                //   400648 (BAE ATP) - Atlantic Airlines
                // altitude == 0, longitude == 0, type == 15 and zeros in latitude LSB.
                // Can alternate with valid reports having type == 14
                //Modes.stats_current.cpr_filtered++;
            }
            else
            {
                // Otherwise, assume it's valid.
                mm.cpr_valid = true;
                mm.cpr_type = CprType.CPR_AIRBORNE;
                mm.cpr_odd = GetBits(mm.ME, 22, 22) != 0;

                if (mm.metype == 18 || mm.metype == 22)
                    mm.cpr_nucp = 0;
                else if (mm.metype < 18)
                    mm.cpr_nucp = (18 - mm.metype);
                else
                    mm.cpr_nucp = (29 - mm.metype);
            }
        }

        if (AC12Field != 0)
        {// Only attempt to decode if a valid (non zero) altitude is present
            mm.altitude = decodeAC12Field(AC12Field, mm.altitude_unit);
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
