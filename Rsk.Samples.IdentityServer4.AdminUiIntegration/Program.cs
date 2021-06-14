using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Build().Run();
        }

        public static IHostBuilder BuildWebHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .ConfigureLogging((context, logging) =>
                        {
                            logging.AddDebug();
                            logging.AddConsole();

                            // Suppress some of the SQL Statements output from EF
                            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                        });
                });
    }
}