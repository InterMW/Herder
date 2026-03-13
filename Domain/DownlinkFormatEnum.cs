namespace Domain;

public enum DownlinkFormat : int
{
    ShortAirAir = 0,
    RollCallReplyAltitude = 4,
    RollCallReplySquawk = 5,
    AllCallReplyAddress = 11,
    LongAirAir = 16,
    ADSB = 17,
    TISB = 18,
    UNUSED = 19,
    AirPositionGNSSHAE = 20,
    RollCallReplyIdentity = 21,
    ExtendedLengthMess = 24
}
