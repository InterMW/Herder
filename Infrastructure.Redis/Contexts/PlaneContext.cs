using MelbergFramework.Infrastructure.Redis;
using Microsoft.Extensions.Options;

namespace Infrastructure.Redis.Contexts;

public class PlaneContext : RedisContext
{
    public PlaneContext( 
            IOptions<RedisConnectionOptions<PlaneContext>> options,
            IConnector connector) : base(options.Value, connector) { }
}
