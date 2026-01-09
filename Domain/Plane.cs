namespace Domain;

public class Plane
{
    public string HexValue {get; set;} = string.Empty;
    public string Squawk {get; set;} = string.Empty;
    public string Flight {get; set;} = string.Empty;
    public float? Latitude {get; set;}
    public float? Longitude {get; set;}
    public string Nucp {get; set;} = string.Empty;
    public int? Altitude {get; set;}
    public int? VerticleRate {get; set;}
    public int? Track {get; set;}
    public int? Speed {get; set;}
    public string Category {get; set;} = string.Empty;
}
