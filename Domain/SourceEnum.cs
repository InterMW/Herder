namespace Domain;

public enum Source
{
    SOURCE_INVALID,        /* data is not valid */
    SOURCE_MLAT,           /* derived from mlat */
    SOURCE_MODE_S,         /* data from a Mode S message, no full CRC */
    SOURCE_MODE_S_CHECKED, /* data from a Mode S message with full CRC */
    SOURCE_TISB,           /* data from a TIS-B extended squitter message */
    SOURCE_ADSB,           /* data from a ADS-B extended squitter message */
}
