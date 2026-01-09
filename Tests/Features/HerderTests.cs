using LightBDD.Framework.Scenarios;
using LightBDD.MsTest3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Features;

[TestClass]
public partial class WranglerTests : BaseTestFrame
{
    [Scenario]
    [TestMethod]
    public async Task DF11()
    {
        await Runner.RunScenarioAsync(
            _ => Setup_scenarios(),
            _ => Planes_comes_in(1),
            _ => The_right_planes_are_there()
                );
    }
    [Scenario]
    [TestMethod]
    [DataRow("00050319AB8C22",4025)]
    public async Task DF00(string packet, int altitude)
    {
        await Runner.RunScenarioAsync(
            _ => Saw_plane("device",0x7C7B5A),
            _ => Message_comes(packet),
            _ => DF00_Handled_Right()
                );
    }


}
