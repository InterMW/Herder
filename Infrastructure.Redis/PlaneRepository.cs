using System.Text.Json;
using Domain;
using Infrastructure.Redis.Contexts;
using MelbergFramework.Infrastructure.Redis;
using StackExchange.Redis;
namespace Infrastructure.Redis;

public interface IPlaneRepository
{
    Task AddIcao(string node, string icao);
    Task<bool> ConfirmIcao(string node, string icao);
    Task RememberIcao(string node, string icao);
    Task RecordPacket(string packet, string icao, long time);
    IAsyncEnumerable<string> GetPackets(string icao, long time);
    IAsyncEnumerable<string> GetIcaosForMoment(long time);
    Task<string> GetNextIcao(long time);
    Task<bool> IcaoMomentSetExists(long time);
    Task PrepareIcao(long time, string icao);
    Task<TimeAnotatedPlane> GetLastSeen(string icao);
}
public class PlaneRepository : RedisRepository<PlaneContext>, IPlaneRepository
{
    private readonly TimeSpan _icaoLiftime = TimeSpan.FromSeconds(60);
    public PlaneRepository(PlaneContext context) : base(context)
    {
    }

    public async Task<TimeAnotatedPlane> GetLastSeen(string icao)
    {
        var result = await DB.StringGetAsync(LastSeenPlaneKey(icao));

        if (result.IsNull)
        {
            return new TimeAnotatedPlane() { HexValue = icao };
        }

        return JsonSerializer.Deserialize<TimeAnotatedPlane>(result!) ?? new TimeAnotatedPlane() { HexValue = icao};
    }

    private static string LastSeenPlaneKey(string icao) => $"last_seen_{icao}";

    public Task<bool> ConfirmIcao(string node, string icao) =>
        DB.KeyExistsAsync(IcaoConfirmationKey(node, icao));

    public async Task RememberIcao(string node, string icao)
    {
        await DB.KeyTouchAsync(IcaoConfirmationKey(node, icao));
        await DB.KeyExpireAsync(IcaoConfirmationKey(node, icao), _icaoLiftime);
    }

    public async Task RecordPacket(string packet, string icao, long time)
    {
        var key = PacketRecordKey(icao, time);
        await DB.ListLeftPushAsync(key, packet);
        await DB.KeyExpireAsync(key, _icaoLiftime);
    }

    private static string PacketRecordKey(string icao, long time) => $"{icao}_{time}";

    public async Task MarkIcaoForMoment(string icao, long time)
    {
        await DB.HashSetAsync(IcaoSeenKey(time), icao, 1);
    }

    public Task<bool> IcaoMomentSetExists(long time) => DB.KeyExistsAsync(IcaoSeenKey(time));

    public async IAsyncEnumerable<string> GetPackets(string icao, long time)
    {
        RedisValue result;
        while ((result = await DB.ListLeftPopAsync(PacketRecordKey(icao, time))).HasValue)
        {
            yield return result!;//works?
        }
    }
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


    static string IcaoConfirmationKey(string node, string icao) => $"icao_{node}_{icao}";

    static string PerNodeSetKey(string node, long time) => $"per_node_{node}_";

    static string SomethingKey(string node, string icao, long time) => $"{node}_{icao}_{time}";
}
