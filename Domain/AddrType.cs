namespace Domain;

public enum AddrType
{
    ADDR_ADSB_ICAO,       /* Mode S or ADS-B, ICAO address, transponder sourced */
    ADDR_ADSB_ICAO_NT,    /* ADS-B, ICAO address, non-transponder */
    ADDR_ADSR_ICAO,       /* ADS-R, ICAO address */
    ADDR_TISB_ICAO,       /* TIS-B, ICAO address */

    ADDR_ADSB_OTHER,      /* ADS-B, other address format */
    ADDR_ADSR_OTHER,      /* ADS-R, other address format */
    ADDR_TISB_TRACKFILE,  /* TIS-B, Mode A code + track file number */
    ADDR_TISB_OTHER,      /* TIS-B, other address format */

    ADDR_UNKNOWN
}
