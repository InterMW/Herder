using Application;
using Application.Processor;
using Infrastructure.Redis;
using Infrastructure.Redis.Contexts;
using LightBDD.MsTest3;
using MelbergFramework.Application;
using MelbergFramework.ComponentTesting.Rabbit;
using MelbergFramework.ComponentTesting.Redis;
using MelbergFramework.Core.ComponentTesting;
using MelbergFramework.Core.DependencyInjection;
using MelbergFramework.Core.Time;
using MelbergFramework.Infrastructure.Rabbit.Consumers;
using MelbergFramework.Infrastructure.Rabbit.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tests.Features;

public class BaseTestFrame : FeatureFixture
{
    public BaseTestFrame()
    {
        App = MelbergHost.CreateHost<AppRegistrator>()
            .AddServices(_ =>
            {
                _.OverrideRedisContext<PlaneContext>();
                _.AddSingleton<ClockProcessor,ClockProcessor>();
                // _.OverrideCouchbaseDatabase();
                _.PrepareConsumer<ClockProcessor>();
                _.OverrideTranslator<ClockMessage>();
                
                _.PrepareConsumer<PacketProcessor>();
                _.OverrideTranslator<PacketMessage>();
                _.OverrideWithSingleton<IClock, MockClock>();
                // _.PrepareConsumer<IndexProcessor>();
                // _.OverrideTranslator<TickMessage>();
                // _.OverrideWithSingleton<IClock,MockClock>();
            })
            .AddControllers()
            .Build();

    }
    public WebApplication App;

    public T GetClass<T>() => (T)App
        .Services
        .GetRequiredService(typeof(T));

    public IPlaneRepository GetPlaneRepository() =>
        App.Services.GetService<IPlaneRepository>()!;

    public ClockProcessor GetClockService() =>
        (ClockProcessor)App
            .Services
            .GetService<ClockProcessor>()!;

    public RabbitMicroService<PacketProcessor> GetPacketService() =>
        (RabbitMicroService<PacketProcessor>)App
            .Services
            .GetServices<IHostedService>()
            .Where(_ => _.GetType() == typeof(RabbitMicroService<PacketProcessor>))
            .First();

    // public RabbitMicroService<IngressProcessor> GetIngressService() =>
    //     (RabbitMicroService<IngressProcessor>)App
    //         .Services
    //         .GetServices<IHostedService>()
    //         .Where(_ => _.GetType() == typeof(RabbitMicroService<IngressProcessor>))
    //         .First();

}
