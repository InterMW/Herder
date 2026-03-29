using DomainService;
using MelbergFramework.Core.Time;
using MelbergFramework.Infrastructure.Rabbit.Consumers;
using MelbergFramework.Infrastructure.Rabbit.Messages;
using MelbergFramework.Infrastructure.Rabbit.Translator;
using Microsoft.Extensions.Logging;

namespace Application.Processor;

public class ClockProcessor(
        IJsonToObjectTranslator<ClockMessage> translator,
        IPacketCoordindatorService domainService,
        IClock clock,
        ILogger<ClockProcessor> logger) : IStandardConsumer
{
    public async Task ConsumeMessageAsync(Message message, CancellationToken ct)
    {
        Console.WriteLine("Hellllllllo");
        Console.WriteLine("Hellllllllo");
        Console.WriteLine("Hellllllllo");
        Console.WriteLine("Hellllllllo");
        Console.WriteLine("Hellllllllo");
        Console.WriteLine("Hellllllllo");
        // Console.WriteLine(message.Body);
        // var dto = translator.Translate(message);
        var now = (long)(clock.GetUtcNow()-DateTime.UnixEpoch).TotalSeconds;
        try
        {
            await domainService.Coordinate(now-3);

        }
        catch (System.Exception ex)
        {

            Console.WriteLine(ex);
            Console.WriteLine(ex);
        }
    }
}

public class ClockMessage : StandardMessage
{
    public long Time { get; set; }
    public override string GetRoutingKey() => "time";
}

