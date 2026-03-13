using LightBDD.MsTest3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Infrastructure;

[TestClass]
class LightBddIntegration
{
    [AssemblyInitialize] public static void Setup(TestContext testContext){ LightBddScope.Initialize(); }
    [AssemblyCleanup] public static void Cleanup(){ LightBddScope.Cleanup(); }
}
