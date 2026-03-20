using Application.Processor;
using DomainService;
using Infrastructure.Rabbit.Publishers;
using Infrastructure.Redis;
using Infrastructure.Redis.Contexts;
using MelbergFramework.Application;
using MelbergFramework.Core.Time;
using MelbergFramework.Infrastructure.Rabbit;
using MelbergFramework.Infrastructure.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public class AppRegistrator : Registrator
{
    public override void RegisterServices(IServiceCollection services)
    {
        RabbitModule.RegisterMicroConsumer<PacketProcessor, PacketMessage>(services, false);
        RabbitModule.RegisterMicroConsumer<ClockProcessor, ClockMessage>(services, true);
        // RabbitModule.RegisterMicroConsumer<TickProcessor, MelbergFramework.Infrastructure.Rabbit.Messages.TickMessage>(services, false);
        // RabbitModule.RegisterMicroConsumer<WorkProcessor, MelbergFramework.Infrastructure.Rabbit.Messages.TickMessage>(services, true);
        RabbitModule.RegisterPublisher<WorkMessage>(services);
        services.AddTransient<IWorkMessagePublisher,WorkMessagePublisher>();
        RedisDependencyModule.LoadRedisRepository<IPlaneRepository, PlaneRepository, PlaneContext>(services);
//        services.AddTransient<IPacketDecoderService, PacketDecoderService>();
        services.AddTransient<IPacketIngestorService, PacketIngestorService>();
        services.AddTransient<IPacketCoordindatorService, PacketCoordindatorService>();
        services.AddSingleton<IClock, Clock>();
    }
}

