using Application.Processor;
using Domain.MessageStrategy;
using DomainService;
using Infrastructure.InfluxDB;
using Infrastructure.Rabbit.Publishers;
using Infrastructure.Redis;
using Infrastructure.Redis.Contexts;
using MelbergFramework.Application;
using MelbergFramework.Core.Time;
using MelbergFramework.Infrastructure.InfluxDB;
using MelbergFramework.Infrastructure.Rabbit;
using MelbergFramework.Infrastructure.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public class AppRegistrator : Registrator
{
    public override void RegisterServices(IServiceCollection services)
    {
        var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        RabbitModule.RegisterMicroConsumer<PacketProcessor, PacketMessage>(services, false);
        RabbitModule.RegisterMicroConsumer<ClockProcessor, ClockMessage>(services, !isDev);
        RabbitModule.RegisterPublisher<WorkMessage>(services);
        RabbitModule.RegisterPublisher<CompletedPlaneFrameMessage>(services);
        services.AddTransient<IWorkMessagePublisher, WorkMessagePublisher>();
        services.AddTransient<ICompletePlaneFramePublisher, CompletePlaneFramePublisher>();
        RedisDependencyModule.LoadRedisRepository<IPlaneRepository, PlaneRepository, PlaneContext>(services);
        services.AddTransient<IPacketIngestorService, PacketIngestorService>();
        services.AddTransient<IPacketCoordindatorService, PacketCoordindatorService>();
        services.AddSingleton<IClock, Clock>();
        InfluxDBDependencyModule.LoadInfluxDBRepository<IPlaneMetadataRepository, PlaneFrameMetadataRepository, InfluxDBContext>(services);


        // Message Strategy
        services.AddSingleton<IMessageStrategy, Strategy00>();
        services.AddSingleton<IMessageStrategy, Strategy04>();
        services.AddSingleton<IMessageStrategy, Strategy05>();
        services.AddSingleton<IMessageStrategy, Strategy17>();

        services.AddSingleton<IMessageDecoder, MessageDecoder>();
    }
}

