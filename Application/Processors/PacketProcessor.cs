using DomainService;
using MelbergFramework.Infrastructure.Rabbit.Consumers;
using MelbergFramework.Infrastructure.Rabbit.Messages;
using MelbergFramework.Infrastructure.Rabbit.Translator;
using Microsoft.Extensions.Logging;

namespace Application.Processor;

public class PacketProcessor(
        IJsonToObjectTranslator<PacketMessage> translator,
        IPacketDecoderService domainService,
        ILogger<PacketProcessor> logger) : IStandardConsumer
{
    public async Task ConsumeMessageAsync(Message message, CancellationToken ct)
    {
        var dto = translator.Translate(message);
        await domainService.DecodePacket(dto.SerialNumber, dto.Frame);
    }
}

public class PacketMessage : StandardMessage
{
    public string SerialNumber { get; set; } = string.Empty;

    public string Frame { get; set; } = string.Empty;

    public override string GetRoutingKey() => "adsbframe";
}
