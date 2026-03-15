using LightBDD.Framework.Scenarios;
using LightBDD.MsTest3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Features;

[TestClass]
public partial class HerderTests : BaseTestFrame
{

    [Scenario]
    [TestMethod]
    [DataRow("8D7C7181215D01A08208204D8BF1", "WPF")]
    public async Task ExtractFlightNumber(string frame, string expected)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => Message_comes("device", $"5F7C7181BB4F87", 10),
            _ => Message_comes("device", frame, 10),
            _ => Clock_strikes(13),
            _ => Expect_Registration("7C7181", "WPF"));

    }

    [Scenario]
    [TestMethod]
    [DataRow("8D40058B58C901375147EFD09357", 49.0f, 6.0f, 49.0f, 6.0f)]
    [DataRow("8D06A15358BF17FF7D4A84B47B95", 30.508474576271183f, (float)(7.2*5.0+3e-15), 30.50540f, 33.447f)]
    public async Task PostitionWithRefTest(string frame, float reflat, float reflon, float expectlat, float expectlon)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => VerifyRefPos(frame, reflat, reflon, expectlat, expectlon)
                );
    }

    [Scenario]
    [TestMethod]
    [DataRow("8D406F7658CD846B3DD4B1995879", false, 13726, 119985)]
    [DataRow("8DA516F15823941B3B82DBE38987", false, 3485, 99035)]
    public async Task SubPosition(string frame, bool isEven, int cprlat, int cprlon)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => VerifyCPR(frame, cprlat, cprlon)
                );

    }

    [Scenario]
    [TestMethod]
    [DataRow("8D75804B580FF2CF7E9BA6F701D0","8D75804B580FF6B283EB7A157117", "75804B", 10.2162144, 123.889128 , 2175)]
    [DataRow("8DA53774583B73A636A7835961B9","8DA53774583B77305323ACD94FC5", "A53774", 41.0, -87.0, 10775)]
    public async Task ExtractPosition(string frame1, string frame2, string icao, double lat, double lon, int altitude)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => Message_comes("device", $"5F{icao}BB4F87", 10),
            _ => Message_comes("device", frame1, 10),
            _ => Message_comes("device", frame2, 10),
            _ => Clock_strikes(13),
            _ => Expect_Position(icao, lat, lon, altitude)
                );
    }

    [Scenario]
    [TestMethod]
    [DataRow("29001B3AF47E76", "7C1474", "3751")]
    public async Task DoThing(string frame, string icao, string squawk)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => Message_comes("device", $"5F{icao}BB4F87", 10),
            _ => Message_comes("device", frame, 10),
            _ => Clock_strikes(13),
            _ => Expect_Squawk(icao, squawk)
                );
    }
    
    [Scenario]
    [TestMethod]
    [DataRow("00050319AB8C22", "7C7B5A", 4025)]
    public async Task DoThing(string frame, string icao, int altitude)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => Message_comes("device", $"5F{icao}BB4F87", 10),
            _ => Message_comes("device", "5F7C7B5ABB4F87", 10),
            _ => Message_comes("device", frame, 10),
            _ => Clock_strikes(13),
            _ => Expect_Altitude(icao, altitude)
                );
    }
    [Scenario]
    [TestMethod]
    [DataRow("5F7C7B5ABB4F87", "7C7B5A")]
    [DataRow("8C7C451C423C52D692D953855472", "7C451C")]
    [DataRow("907CF7C7C1000000001F04BF815D", "7CF7C7")]
    public async Task ConfirmIcaoFilteringForIcaoPresent(string frame, string icao)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => Message_comes("device",frame, 4),
            _ => Icao_was_seen("device",icao)
        );
    }
     
    [Scenario]
    [TestMethod]
    [DataRow("00050319AB8C22", "7C7B5A")]
    [DataRow("200006A2DE8B1C", "7C1B28")]
    [DataRow("29001B3AF47E76", "7C1474")]
    [DataRow("8061942058A20AA10C3A1E6EE7CD", "7C431F")]
    [DataRow("A000019D10000800F000004635C0", "7C7F0D")]
    [DataRow("A8000F0D10010080FD0000A892C2", "7C1C70")]
    [DataRow("C482DD4F219709344D55CE7F0811", "7C1C70")]
    public async Task ConfirmIcaoAcceptenceForIcaoEncoded(string frame, string icao)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => Message_comes("device", $"5F{icao}BB4F87", 4),
            _ => Message_comes("device", frame, 4),
            _ => Icao_was_seen("device", icao)
        );
    }

    [Scenario]
    [TestMethod]
    [DataRow("00050319AB8C22", "7C7B5A")]
    [DataRow("200006A2DE8B1C", "7C1B28")]
    [DataRow("29001B3AF47E76", "7C1474")]
    [DataRow("8061942058A20AA10C3A1E6EE7CD", "7C431F")]
    [DataRow("A000019D10000800F000004635C0", "7C7F0D")]
    [DataRow("A8000F0D10010080FD0000A892C2", "7C1C70")]
    [DataRow("C482DD4F219709344D55CE7F0811", "7C1C70")]
    public async Task ConfirmIcaoRejectionForIcaoEncoded(string frame, string icao)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => Message_comes("device", frame, 4),
            _ => Icao_was_not_seen("device", icao)
        );

    }
    [Scenario]
    [TestMethod]
    [DataRow("00050319AB8C22", "7C7B5A")]
    public async Task ConfirmMessageDeduplication(string frame, string icao)
    {
        await Runner.RunScenarioAsync(
            _ => Init_test(),
            _ => Setup_scenarios(),
            _ => Message_comes("device", frame, 10),
            _ => Message_comes("device", $"5F{icao}BB4F87", 10),
            _ => Message_comes("device", frame, 10),
            _ => Message_comes("device2", $"5F{icao}BB4F87", 10),
            _ => Expect_packages(icao, 10,2)
        );
    }
}
