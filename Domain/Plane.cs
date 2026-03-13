namespace Domain;

public class Plane
{
    public string HexValue {get; set;} = string.Empty;
    public string Squawk {get; set;} = string.Empty;
    public bool SquawkValid {get; set;} = false;
    public string Flight {get; set;} = string.Empty;
    public float? Latitude {get; set;}
    public float? Longitude {get; set;}
    public string Nucp {get; set;} = string.Empty;
    public int? Altitude {get; set;}
    public int? VerticleRate {get; set;}
    public int? Track {get; set;}
    public int? Speed {get; set;}
    public string Category {get; set;} = string.Empty;
    public string[] PositionMessage = new string[2];
    public long[] PositionTimestamp = new long[2] { 0, 0};
    public long TPos;
}
