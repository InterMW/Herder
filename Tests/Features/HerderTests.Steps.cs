using Application;
using Application.Processor;
using DomainService;
using Infrastructure.Redis;
using MelbergFramework.ComponentTesting.Rabbit;
using MelbergFramework.Core.ComponentTesting;
using MelbergFramework.Core.Time;
using MelbergFramework.Infrastructure.Rabbit.Extensions;
using MelbergFramework.Infrastructure.Rabbit.Messages;
using MelbergFramework.Infrastructure.Rabbit.Translator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Features;

public partial class WranglerTests : BaseTestFrame
{
    private Dictionary<int, PacketMessage> Scenarios =
        new Dictionary<int, PacketMessage>();

    private IActionResult _response;

    public async Task Setup_scenarios()
    {
        Scenarios.Add(0, new PacketMessage()
                {
        Frame = "00050319AB8C22",
        SerialNumber = "device"
                });
        Scenarios.Add(11, new PacketMessage()
        {
            Frame = "5F7C7B5ABB4F87",
            SerialNumber = "device"
        });

    }
    public async Task Planes_comes_in(int scenario)
    {
        var mockTranslator = (MockTranslator<PacketMessage>)GetClass<IJsonToObjectTranslator<PacketMessage>>();
        mockTranslator.Messages.Add(Scenarios[scenario]);
        await GetPacketService().ConsumeMessageAsync(new Message(), CancellationToken.None);
    }

    public async Task Message_comes(string packet, int second)
    {
        var mockClock = (MockClock)this.GetClass<IClock>();
        mockClock.NewCurrentTime = DateTime.UnixEpoch.AddSeconds(second);
        var mockTranslator = (MockTranslator<PacketMessage>)GetClass<IJsonToObjectTranslator<PacketMessage>>();
        mockTranslator.Messages.Add(new PacketMessage(){   SerialNumber = "device", Frame = packet});
        await GetPacketService().ConsumeMessageAsync(new Message(), CancellationToken.None);

    }

    // public async Task Get_planes()
    // {
    //     var clock = (MockClock)GetClass<IClock>();
    //     clock.NewCurrentTime =DateTime.UnixEpoch.AddSeconds(7); 
    //     var controller = new WranglerController(
    //             GetClass<IAccessDomainService>(),
    //             GetClass<IOptions<TimingsOptions>>(),
    //             GetClass<IClock>(),
    //             GetClass<ILogger<WranglerController>>());


    //    _response = await controller.GetFrameAsync(4);
    // }
    public async Task Saw_plane(string node, uint icao)
    {


        var repo = this.GetClass<IPlaneRepository>();
        await repo.AddIcao(node, icao);
    }
    public async Task DF00_Handled_Right()
    {
        var repo = this.GetClass<IPlaneRepository>();

        var result = await repo.GetValidIcaos("device");

    }

    public async Task The_right_planes_are_there()
    {
        var repo = this.GetClass<IPlaneRepository>();

        var result = await repo.GetValidIcaos("device");
        Assert.IsTrue(result.Any());
        Assert.AreEqual((uint)result.First(),(uint) 0x7C7B5A);
    }
}
