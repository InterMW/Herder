using MelbergFramework.Infrastructure.InfluxDB;

namespace Infrastructure.InfluxDB;

public interface IPlaneMetadataRepository
{
    Task LogPlaneMetadata(int total, DateTime timestamp);
    Task LogNodeMetadata(string serialNumber, int total, DateTime timestamp);
}

public class PlaneFrameMetadataRepository : BaseInfluxDBRepository<InfluxDBContext>, IPlaneMetadataRepository
{
    public PlaneFrameMetadataRepository(InfluxDBContext context) : base(context) { }

    public Task LogPlaneMetadata(int total, DateTime timestamp) => 
        Context.WritePointAsync(
            PlaneFrameMetadataMapper.ToDataModel(total, timestamp),
            "plane_data",
            "Inter");

    public Task LogNodeMetadata(string serialNumber, int total, DateTime timestamp) =>
        Context.WritePointAsync(
            PlaneFrameMetadataMapper.ToDataModel(serialNumber, total, timestamp),
            "plane_data",
            "Inter");
}

public static class PlaneFrameMetadataMapper 
{
    public static InfluxDBDataModel ToDataModel(string serialNumber, int total, DateTime timestamp)
    {
        var result = new InfluxDBDataModel("plane_metadata");

        result.Tags["class"] = "input";
        result.Tags["serial"] = serialNumber;
        result.Fields["total"] = total;
        result.Timestamp = timestamp;
        return result;
    }
    public static InfluxDBDataModel ToDataModel(int total, DateTime timestamp)
    {
        var result = new InfluxDBDataModel("plane_metadata");


        result.Tags["class"] = "result";
        result.Fields["total"] = total;
        result.Timestamp = timestamp;
        return result;
    }
}
