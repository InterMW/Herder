using System.Text.Json.Serialization;
using Infrastructure.Redis;
using MelbergFramework.Core.Time;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

[ApiController]
[Route("[controller]")]
public class PlaneFrameController(IPlaneRepository _service, IClock _clock)
{
    [HttpGet]
    [Route("frame")]
    public async Task<PlaneFrameResponse> GetFrame([FromQuery] long? time)
    {

        var usedtime = time ?? (long)(_clock.GetUtcNow() - DateTime.UnixEpoch).TotalSeconds -5;
        var result = await _service.GetSkyFrame(usedtime);

        return new PlaneFrameResponse()
        {
            Now = usedtime,
            Planes = result.Planes.Where(_ => _.Latitude != _.Longitude).Select(_ => new PlaneResponse() 
                    {
                        HexValue = _.HexValue,
                        Latitude = _.Latitude,
                        Longitude = _.Longitude,
                        Altitude = _.Altitude,
                        Flight = _.Flight,
                        Track = _.Track,
                        Speed = _.Speed,
                        Squawk = _.Squawk,
                        Category = _.Category,

                    }).ToArray(),
        };
    }
}

public class PlaneFrameResponse 
{
    [JsonPropertyName("now")] 
    public long Now {get; set;}
    [JsonPropertyName("planes")] 
    public PlaneResponse[] Planes {get; set;} = Array.Empty<PlaneResponse>();
    [JsonPropertyName("antenna")] 
    public string Antenna {get; set;} = string.Empty;
    [JsonPropertyName("source")] 
    public string Source {get; set;} = string.Empty;
}

public class PlaneResponse 
{
    [JsonPropertyName("hexValue")] 
    public string HexValue {get; set;} = string.Empty;
    [JsonPropertyName("squawk")]
    public string Squawk {get; set;} = string.Empty;
    [JsonPropertyName("flight")]
    public string Flight {get; set;} = string.Empty;
    [JsonPropertyName("lat")]
    public float? Latitude {get; set;}
    [JsonPropertyName("lon")]
    public float? Longitude {get; set;}
    [JsonPropertyName("nucp")]
    public string Nucp {get; set;} = string.Empty;
    [JsonPropertyName("altitude")]
    public int? Altitude {get; set;}
    [JsonPropertyName("vert_rate")]
    public int? VerticleRate {get; set;}
    [JsonPropertyName("track")]
    public int? Track {get; set;}
    [JsonPropertyName("speed")]
    public int? Speed {get; set;}
    [JsonPropertyName("category")]
    public string Category {get; set;} = string.Empty;
    [JsonPropertyName("messages")]
    public string Messages {get; set;} = string.Empty;
    [JsonPropertyName("rssi")] 
    public float? Rssi {get;set;}
}
