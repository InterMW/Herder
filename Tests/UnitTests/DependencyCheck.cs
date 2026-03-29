using Application;
using Domain.MessageStrategy;
using MelbergFramework.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.UnitTests;

[TestClass]
public class DependencyCheck
{
    [TestMethod]
    public void VerifyNoDupicateStrategies()
    {
        var App = MelbergHost.CreateHost<AppRegistrator>().Build();
        var strategies = App.Services.GetServices<IMessageStrategy>();

        Assert.AreEqual(strategies.ToDictionary(_ => _.DownlinkFormat).Keys.Count(), strategies.Count());
    }
}

//make a unit test that tests 00
