using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using DomainService;
using Microsoft.Extensions.Logging;
using Prometheus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Application.Processor;

public class PacketProcessor( IPacketIngestorService domainService, ILogger<PacketProcessor> logger) : BackgroundService
{ 
    private static readonly Histogram PacketTimer = Metrics
    .CreateHistogram("herder_packettime_duration_seconds", "Histogram of packet processing durations.");


    static Encoding encoding = Encoding.GetEncoding("Utf-8");
    // What problems am I trying to solve.
    //
    // Need to keep local/prod seprably easy style
    // need to make it testable easy style
    //

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //Consider making this its own thing
        
        ConnectionFactory factory = new ConnectionFactory
        {
            Uri = new Uri("amqp://user:pass@hostName:port/vhost")
        };

        IConnection conn = await factory.CreateConnectionAsync(stoppingToken);

        //channel is not thread safe
        IChannel channel = await conn.CreateChannelAsync(cancellationToken: stoppingToken);

        string exchange = "amq.topic";
        string queue = "herder_package";

        await channel.ExchangeDeclareAsync(exchange, "Topic", true, false, cancellationToken: stoppingToken);

        var _ = await channel.QueueDeclareAsync(queue, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(exchange, queue, "node.frame", cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (ch, ea) =>
        {
            using(PacketTimer.NewTimer())
            {
              var ms = new MemoryStream(ea.Body.ToArray());
              var reader = new StreamReader(ms, encoding, false);
              string content = reader.ReadToEnd();

              try
              {
                  var result = JsonSerializer.Deserialize<PacketMessage>(content);
                  await domainService.RecordPacket(result.SerialNumber, result.Frame);
              }
              catch (Exception ex)
              {
                  logger.LogError(ex, "Failed to process Packet");
              }

              await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
        };

        var scale = 1;

        var consumers = await Task.WhenAll(
            Enumerable
              .Range(0,scale)
              .Select(_ => channel.BasicConsumeAsync(queue, false, consumer)));

        // wait until the end
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);

        // close all the consumers
        await Task.WhenAll(consumers.Select(tag => channel.BasicCancelAsync(tag)));

    }
}

public class PacketMessage 
{
    public string SerialNumber { get; set; } = string.Empty;

    public string Frame { get; set; } = string.Empty;
}
