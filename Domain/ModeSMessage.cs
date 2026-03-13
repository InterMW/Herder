namespace Domain;

public class ModeSMessage
{
    public ModeSMessage(string frame)
    {
        Frame = frame;
        this.MessageBits = Frame
            .Select(_ => int.Parse($"{_}", System.Globalization.NumberStyles.HexNumber))
            .Select(_ => string.Format("{0:b4}",_))
            .SelectMany(_ => _)
            .Select(_ => _ == '1')
            .ToArray();
    }
    public uint aa1;
    public uint aa2;
    public uint aa3;
    public int ca;              /* Responder capabilities. */
        //unsigned char msg[MODES_LONG_MSG_BYTES];      // Binary message.
    //unsigned char verbatim[MODES_LONG_MSG_BYTES]; // Binary message, as originally received before correction
    int           msgbits;                        // Number of bits in message 
    int           msgtype;                        // Downlink format #
    // uint32_t      crc;                            // Message CRC
    int           correctedbits;                  // No. of bits corrected 
    public UInt32      addr;                           // Address Announced
    public AddrType    addrtype;                       // address format / source
    public UInt64      timestampMsg;                   // Timestamp of the message (12MHz clock)
    //struct timespec sysTimestampMsg;              // Timestamp of the message (system time)
    int           remote;                         // If set this message is from a remote station
    double        signalLevel;                    // RSSI, in the range [0..1], as a fraction of full-scale power
    int           score;                          // Scoring from scoreModesMessage, if used

    public DataSource source;                         // Characterizes the overall message source

    public int IID; // extracted from CRC of DF11s
    public int AA;
    public int AC;
    public int CA;
    public int CC;
    public int CF;
    public int DR;
    public int FS;
    public int ID;
    public int KE;
    public int ND;
    public int RI;
    public int SL;
    public int UM;
    public int VS;
    public char[] MB = new char[7];
    public char[] MD = new char[10];
    public char[] MV = new char[7];
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
    public string ME = string.Empty;
    public char[] flight = new char[9];             /* 8 chars flight number. */
    public int ew_dir;                 /* 0 = East, 1 = West. */
    public int ew_velocity;            /* E/W velocity. */
    public int ns_dir;                 /* 0 = North, 1 = South. */
    public int ns_velocity;            /* N/S velocity. */
    public int vert_rate_sign;         /* Vertical rate sign. */
    public int vert_rate;              /* Vertical rate. */
    public int velocity;               /* Computed from EW and NS velocity. */

    public int MessageType;
    public bool[] MessageBits;
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

    public string Frame { get; set; } = string.Empty;
     public bool altitude_valid = false;
    public bool heading_valid = false;
    public bool speed_valid = false;
    public bool vert_rate_valid = false;
    public bool squawk_valid = false;
    public bool callsign_valid = false;
    public bool ew_velocity_valid = false;
    public bool ns_velocity_valid = false;
    public bool cpr_valid = false;
    public bool cpr_odd = false;
    public bool cpr_decoded = false;
    public bool cpr_relative = false;
    public bool category_valid = false;
    public bool gnss_delta_valid = false;
    public bool from_mlat = false;
    public bool from_tisb = false;
    public bool spi_valid = false;
    public bool spi = false;
    public bool alert_valid = false;
    public bool alert = false;

    
    // valid if altitude_valid:
    public int               altitude;         // Altitude in either feet or meters
    public AltitudeUnit   altitude_unit;    // the unit used for altitude
    public AltitudeSource altitude_source;  // whether the altitude is a barometric altude or a GNSS height
    // valid if gnss_delta_valid:
    public int               gnss_delta;       // difference between GNSS and baro alt
    // valid if heading_valid:
    public HeadingSource  heading_source;   // what "heading" is measuring (true or magnetic heading)
    // valid if speed_valid:
    public uint          speed;            // in kts, reported by aircraft, or computed from from EW and NS velocity
    public SpeedSource    speed_source;     // what "speed" is measuring (groundspeed / IAS / TAS)
    // valid if vert_rate_valid:
    public AltitudeSource vert_rate_source; // the altitude source used for vert_rate
    // valid if squawk_valid:
    public uint          squawk;           // 13 bits identity (Squawk), encoded as 4 hex digits
    // valid if callsign_valid
    public char[]              callsign = new char[9];      // 8 chars flight number
    // valid if category_valid
    public int category;          // A0 - D7 encoded as a single hex byte
    // valid if cpr_valid
    public CprType cpr_type;        // The encoding type used (surface, airborne, coarse TIS-B)
    public int cpr_lat;           // Non decoded latitude.
    public int cpr_lon;           // Non decoded longitude.
    public int cpr_nucp;          // NUCp/NIC value implied by message type

    public AirGround airground;      // air/ground state

    // valid if cpr_decoded:
    public double decoded_lat;
    public double decoded_lon;
}
