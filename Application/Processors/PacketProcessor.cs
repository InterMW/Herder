using Domain;
using DomainService;
using MelbergFramework.Infrastructure.Rabbit.Consumers;
using MelbergFramework.Infrastructure.Rabbit.Messages;
using MelbergFramework.Infrastructure.Rabbit.Translator;

namespace Application.Processor;

// public class PacketProcessor(
//         IJsonToObjectTranslator<PacketMessage> translator,
//         IThermalDomainService domainService,
//         ILogger<ThermalProcessor> logger) : IStandardConsumer
// {
//     public async Task ConsumeMessageAsync(Message message, CancellationToken ct)
//     {
//         var dto = translator.Translate(message);
//     }
// }

// public class PacketMessage : StandardMessage
// {
//     public string SerialNumber { get; set; } = string.Empty;

//     public DateTime Timestamp { get; set; }

//     public string Frame { get; set; } = string.Empty;

//     public override string GetRoutingKey() => "adsbframe";
// }
