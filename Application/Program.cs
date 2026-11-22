using Application;
using MelbergFramework.Application;

ThreadPool.SetMinThreads(8,8);


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(name: "MyPolicy",
            builder => builder.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed((host) => host switch 
                  {
                      "http://localhost:1808" or 
                      "https://wip.centurionx.net" or
                      "https://www.centurionx.net"  or
                      "https://plane.centurionx.net" => true,
                     _ => false
                  }
              )
        );
    }
);      


await MelbergHost
    .CreateHost<AppRegistrator>()
    .DevelopmentPasswordReplacement("Rabbit:ClientDeclarations:Connections:0:Password", "rabbit_pass")
    .AddControllers()
    .Build()
    .RunAsync();
