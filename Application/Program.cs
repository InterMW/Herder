using Application;
using MelbergFramework.Application;

// new ModeSMessage("5F7C7B5ABB4F87");
// new ModeSMessage("5D7C431FBE0A77");
// PlaneFrameDecoder.DecodeModesFrame("00050319AB8C22");
// PlaneFrameDecoder.DecodeModesFrame("5D7C1B28ACC729");

internal class Program
{
    private static async Task Main(string[] args)
    {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        await MelbergHost
            .CreateHost<AppRegistrator>()
            .DevelopmentPasswordReplacement("Rabbit:ClientDeclarations:Connections:0:Password", "rabbit_pass")
            .Build()
            .RunAsync();
    }
}
