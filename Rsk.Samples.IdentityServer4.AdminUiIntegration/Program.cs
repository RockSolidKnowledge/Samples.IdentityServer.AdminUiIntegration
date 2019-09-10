using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(loggingConfiguration =>
                {
                    loggingConfiguration.AddDebug();
                    loggingConfiguration.AddConsole();
                    loggingConfiguration.SetMinimumLevel(LogLevel.Debug);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls("http://*:5003")
                .UseIISIntegration()
                .UseKestrel()
                .Build();
    }
}

