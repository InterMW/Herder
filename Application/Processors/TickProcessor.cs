using DomainService;
using MelbergFramework.Infrastructure.Rabbit.Consumers;
using MelbergFramework.Infrastructure.Rabbit.Extensions;
using MelbergFramework.Infrastructure.Rabbit.Messages;
using Microsoft.Extensions.Logging;

namespace Application.Processor;

public class TickProcessor(
        ICoordinatorService domainService,
        ILogger<TickProcessor> logger) : IStandardConsumer
{
    public async Task ConsumeMessageAsync(Message message, CancellationToken ct)
    {
        var time = ExtractTimestamp(message.GetTimestamp());
        await domainService.Coordinate(time);
    }

    private long ExtractTimestamp(DateTime time) =>
        (long)Math.Floor(time.Subtract(DateTime.UnixEpoch).TotalSeconds);
}

