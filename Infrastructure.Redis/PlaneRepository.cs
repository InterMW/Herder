using System.Text.Json;
using Domain;
using Infrastructure.Redis.Contexts;
using MelbergFramework.Infrastructure.Redis;
using StackExchange.Redis;
namespace Infrastructure.Redis;

public interface IPlaneRepository
{
    Task<Plane> GetPlane(string icao);
    Task UpdatePlane(Plane plane);
    Task<string> GetNextPacket(string icao, long time);
    Task<bool> IsNewMessage(string frame);
    IAsyncEnumerable<string> GetPackets();
    Task AddIcao(string node, string icao);
    Task<bool> ConfirmIcao(string node, string icao);
    Task RememberIcao(string node, string icao);
    Task RecordPacket(string packet, string icao, long time);
    Task MarkIcaoForMoment(string icao, long time);
    IAsyncEnumerable<string> GetIcaosForMoment(long time);
    Task<string> GetNextIcao(long time);
    Task SaveFrame(SkyFrame frame);
    Task<SkyFrame> GetSkyFrame(long time);
    Task<bool> IcaoMomentSetExists(long time);
    Task PrepareIcao(long time, string icao);
    Task<TimeAnotatedPlane> GetLastSeen(string icao);
    Task<long> GetCompleteIcaoCount(long time);
    Task MarkIcaoMomentAsComplete(string icao, long time);
}
public class PlaneRepository : RedisRepository<PlaneContext>, IPlaneRepository
{
    private readonly TimeSpan _icaoLiftime = TimeSpan.FromSeconds(60);

    public PlaneRepository(PlaneContext context) : base(context) { }

    public async Task<bool> IsNewMessage(string frame)
    {
        var added = await DB.HashSetAsync(ProcessedMessageKey(), frame, "a", When.NotExists);
        if (added)
        {
            await DB.HashFieldExpireAsync(ProcessedMessageKey(), new RedisValue[] { frame }, TimeSpan.FromSeconds(1));
        }
        else
        {
            //Console.WriteLine($"{frame} Is not  new message");
        }

        return added;
    }

    private static string ToSkyFrameKey(long time) => $"skyframe_{time}";
    public async Task<SkyFrame> GetSkyFrame(long time)
    {
        var key = ToSkyFrameKey(time);
        var result = await DB.StringGetAsync( key);
        if (!result.HasValue)
        {
            return new SkyFrame { Timestamp = time };
        }
        return JsonSerializer.Deserialize<SkyFrame>(result) ;
    }
    public async Task SaveFrame(SkyFrame frame)
    {
        var key = ToSkyFrameKey(frame.Timestamp);
        await DB.StringSetAsync( key, JsonSerializer.Serialize<SkyFrame>(frame));
        await DB.KeyExpireAsync(key, _icaoLiftime);
    }

    public async Task<Plane> GetPlane(string icao)
    {
        var result = await DB.StringGetAsync(PlaneKey(icao));

        if (!result.HasValue)
        {
            return new Plane(){ HexValue = icao};
        }


        return JsonSerializer.Deserialize<Plane>(result!)!;
    }

    public async Task UpdatePlane(Plane plane)
    {
        if(plane.PositionTimestamp.Any(_ => _ != 0))
        {
            //Console.WriteLine(JsonSerializer.Serialize<Plane>(plane));
        }
        await DB.StringSetAsync(PlaneKey(plane.HexValue),JsonSerializer.Serialize<Plane>(plane));
    }

    private string PlaneKey(string icao) => $"plane_{icao}";


    public async IAsyncEnumerable<string> GetPackets()
    {
        await foreach (var result in DB.HashScanNoValuesAsync(ProcessedMessageKey()))
        {
            yield return result;//works?
        }
    }

    private static string ProcessedMessageKey() => $"seen";


    public async Task<TimeAnotatedPlane> GetLastSeen(string icao)
    {
        var result = await DB.StringGetAsync(LastSeenPlaneKey(icao));

        if (result.IsNull)
        {
            return new TimeAnotatedPlane() { HexValue = icao };
        }

        return JsonSerializer.Deserialize<TimeAnotatedPlane>(result!) ?? new TimeAnotatedPlane() { HexValue = icao };
    }

    private static string LastSeenPlaneKey(string icao) => $"last_seen_{icao}";

    public async Task<bool> ConfirmIcao(string node, string icao)
    {

        // Console.WriteLine($"touch did i ? {node} {icao}");
        var result = await DB.KeyExistsAsync(IcaoConfirmationKey(node, icao));
        // Console.WriteLine($"{result}");
        return result;
    }

    public async Task RememberIcao(string node, string icao)
    {
        //Console.WriteLine($"I touched {node} {icao}");
        await DB.StringSetAsync(IcaoConfirmationKey(node, icao), "a");
        await DB.KeyExpireAsync(IcaoConfirmationKey(node, icao), _icaoLiftime);
    }

    public async Task RecordPacket(string packet, string icao, long time)
    {
        var key = PacketRecordKey(icao, time);
        await DB.ListLeftPushAsync(key, packet);
        await DB.KeyExpireAsync(key, _icaoLiftime);
    }

    public async Task<string> GetNextPacket(string icao, long time) =>
        (await DB.ListRightPopAsync(PacketRecordKey(icao, time))).ToString();

    private static string PacketRecordKey(string icao, long time) => $"{icao}_{time}";

    public async Task MarkIcaoForMoment(string icao, long time)
    {
        //Console.WriteLine(IcaoSeenKey(time) + " " + icao);
        await DB.HashSetAsync(IcaoSeenKey(time), icao, 1);
    }

    public Task<bool> IcaoMomentSetExists(long time) => DB.KeyExistsAsync(IcaoSeenKey(time));

    public async IAsyncEnumerable<string> GetIcaosForMoment(long time)
    {
        await foreach (var result in DB.HashScanNoValuesAsync(IcaoSeenKey(time)))
        {
            yield return result;//works?
        }
    }

    private static string IcaoSeenKey(long time) => $"seen_{time}";

    public async Task PrepareIcao(long time, string icao)
    {
        await DB.ListRightPushAsync(PreparedIcaosKey(time), icao);
    }
    public async Task<string> GetNextIcao(long time)
    {
        var result = await DB.ListLeftPopAsync(PreparedIcaosKey(time));

        //completely untested
        return result.HasValue ? result.ToString() : string.Empty;
    }

    private static string PreparedIcaosKey(long time) => $"prepared_{time}";

    public async Task AddIcao(string node, string icao)
    {
        await DB.HashFieldSetAndSetExpiryAsync(ToIcaoSetString(node), icao, "1", _icaoLiftime);
    }

    static string ToIcaoSetString(string node) => $"{node}_icao_set";

    public async Task MarkIcaoMomentAsComplete(string icao, long time)
    {
        var key = PreparedIcaosKey(time);
        await DB.ListRightPushAsync(key, icao);
        await DB.KeyExpireAsync(key, _icaoLiftime);
    }

    public Task<long> GetCompleteIcaoCount(long time) =>
        DB.SetLengthAsync(PreparedIcaosKey(time));

    private static string CompletedIcaosKey(long time) => $"complete_{time}";

    static string IcaoConfirmationKey(string node, string icao) => $"icao_{node}_{icao}";

    static string PerNodeSetKey(string node, long time) => $"per_node_{node}_";

    static string SomethingKey(string node, string icao, long time) => $"{node}_{icao}_{time}";
}
