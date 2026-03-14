using Application;
using Application.Processor;
using Domain;
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

public partial class HerderTests : BaseTestFrame
{
    private Dictionary<int, PacketMessage> Scenarios =
        new Dictionary<int, PacketMessage>();

    private IActionResult _response;

    public async Task Init_test()
    {
        await GetClass<RedisFixture>().InitializeAsync();
    }

    public async Task Setup_scenarios()
    {
        await Task.CompletedTask;
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
    public async Task Message_comes(string device, string packet, int second)
    {
        var mockClock = (MockClock)this.GetClass<IClock>();
        mockClock.NewCurrentTime = DateTime.UnixEpoch.AddSeconds(second);
        var mockTranslator = (MockTranslator<PacketMessage>)GetClass<IJsonToObjectTranslator<PacketMessage>>();
        mockTranslator.Messages.Add(new PacketMessage() { SerialNumber = device, Frame = packet });
        await GetPacketService().ConsumeMessageAsync(new Message(), CancellationToken.None);
    }
    public async Task Expect_Registration(string icao, string registration)
    {
        var plane = await GetPlaneRepository().GetPlane(icao);
        Console.Write(plane.Flight + ":" + registration);
        Assert.AreSame(registration,plane.Flight);
    }

    public async Task Expect_Position(string icao, double lat, double lon, int altitude)
    {

        var plane = await GetPlaneRepository().GetPlane(icao);
        Console.WriteLine($"{lat} vs {plane.Latitude.Value}");
        Console.WriteLine($"{lon} vs {plane.Longitude.Value}");
        Console.WriteLine($"{altitude} vs {plane.Altitude.Value}");
        Assert.IsTrue(Math.Abs(lat- plane.Latitude.Value) < 1);
        Assert.IsTrue(Math.Abs(lon - plane.Longitude.Value) < 1);
        Assert.IsTrue(Math.Abs(altitude - plane.Altitude.Value) < 1);
    }

    public async Task VerifyCPR(string frame, int lat, int lon)
    {
        var (resultLat, resultLon) = PlaneFrameDecoder.ExtractCprLatLon(frame);

        Assert.AreEqual(resultLon, lon);
        Assert.AreEqual(resultLat, lat);

        await Task.CompletedTask;
    }

    public async Task Expect_Squawk(string icao, string squawk)
    {

        var plane = await GetPlaneRepository().GetPlane(icao);
        Console.WriteLine(squawk + " = " + plane.Squawk);
        Assert.AreEqual(squawk, plane.Squawk);
    }

    public async Task Expect_Altitude(string icao, int altitude)
    {

        var plane = await GetPlaneRepository().GetPlane(icao);
        Console.WriteLine(altitude + " = " + plane.Altitude);
        Assert.AreEqual(altitude, plane.Altitude);
    }

    public async Task Clock_strikes(int second)
    {
        var mockClock = (MockClock)this.GetClass<IClock>();
        mockClock.NewCurrentTime = DateTime.UnixEpoch.AddSeconds(second);
        var mockTranslator = (MockTranslator<ClockMessage>)GetClass<IJsonToObjectTranslator<ClockMessage>>();
        mockTranslator.Messages.Add(new ClockMessage() { Time = second});
        await GetClockService().ConsumeMessageAsync(new Message(), CancellationToken.None);
    }

    public async Task Icao_was_not_seen(string device, string icao)
    {
        var repo = this.GetClass<IPlaneRepository>();
        Assert.IsFalse(await repo.ConfirmIcao(device, icao));
    }

    public async Task Icao_was_seen(string device, string icao)
    {
        var repo = this.GetClass<IPlaneRepository>();
        Assert.IsTrue(await repo.ConfirmIcao(device, icao));
    }

    public async Task Expect_packages(string icao, long time, int expectedNumber)
    {
        var repo = this.GetClass<IPlaneRepository>();
        var count = 0;

        await foreach (var message in repo.GetPackets())
        {
            Console.WriteLine(message);
            count++;
        }

        Assert.AreEqual(expectedNumber, count);
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
    }
    public async Task DF00_Handled_Right()
    {
        var repo = this.GetClass<IPlaneRepository>();

        // var result = await repo.GetValidIcaos("device");

    }

    public async Task The_right_planes_are_there()
    {
        var repo = this.GetClass<IPlaneRepository>();

        // var result = await repo.GetValidIcaos("device");
        // Assert.IsTrue(result.Any());
        // Assert.AreEqual((uint)result.First(),(uint) 0x7C7B5A);
    }
}
